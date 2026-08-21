using System.Text.Json;
using AggregatorService.Dtos;
using AggregatorService.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AggregatorService.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize]
public class IntegrationController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IntegrationOptions _integrationOptions;
    private readonly ILogger<IntegrationController> _logger;

    public IntegrationController(
        IHttpClientFactory httpClientFactory,
        IOptions<IntegrationOptions> integrationOptions,
        ILogger<IntegrationController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _integrationOptions = integrationOptions.Value;
        _logger = logger;
    }

    [HttpGet("providers")]
    [ProducesResponseType(typeof(IntegrationProvidersResponseDto), StatusCodes.Status200OK)]
    public ActionResult<IntegrationProvidersResponseDto> GetProviders()
    {
        return Ok(new IntegrationProvidersResponseDto
        {
            Translators =
            [
                new IntegrationProviderOptionDto { Id = "mymemory", DisplayName = "MyMemory (free)" },
            ],
            Dictionaries =
            [
                new IntegrationProviderOptionDto { Id = "freedictionary", DisplayName = "Free Dictionary API" },
            ],
        });
    }

    [HttpPost("translate")]
    [ProducesResponseType(typeof(TranslateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<TranslateResponseDto>> Translate(
        [FromBody] TranslateRequestDto request,
        CancellationToken cancellationToken)
    {
        var text = request.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest(new { error = "Text is required." });
        }

        var provider = (request.Provider ?? string.Empty).Trim().ToLowerInvariant();
        if (provider != "mymemory")
        {
            return BadRequest(new { error = $"Unsupported translator provider: {request.Provider}" });
        }

        var source = NormalizeLang(request.SourceLanguage, "en");
        var target = NormalizeLang(request.TargetLanguage, "ru");
        var q = Uri.EscapeDataString(text);
        var langPair = Uri.EscapeDataString($"{source}|{target}");
        var baseUrl = (_integrationOptions.MyMemoryBaseUrl ?? "https://api.mymemory.translated.net").TrimEnd('/');
        var url = $"{baseUrl}/get?q={q}&langpair={langPair}";

        var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MyMemory translation failed: {StatusCode} {Body}", response.StatusCode, body);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Translator provider request failed." });
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<MyMemoryResponseDto>(body, JsonOptions);
            var translated = parsed?.ResponseData?.TranslatedText?.Trim();
            if (string.IsNullOrWhiteSpace(translated))
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "Translator returned empty text." });
            }

            return Ok(new TranslateResponseDto
            {
                Provider = provider,
                TranslatedText = translated,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse MyMemory response");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Translator response parse error." });
        }
    }

    [HttpPost("dictionary/lookup")]
    [ProducesResponseType(typeof(DictionaryLookupResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<DictionaryLookupResponseDto>> LookupDictionary(
        [FromBody] DictionaryLookupRequestDto request,
        CancellationToken cancellationToken)
    {
        var word = request.Word?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(word))
        {
            return BadRequest(new { error = "Word is required." });
        }

        var provider = (request.Provider ?? string.Empty).Trim().ToLowerInvariant();
        if (provider != "freedictionary")
        {
            return BadRequest(new { error = $"Unsupported dictionary provider: {request.Provider}" });
        }

        var language = NormalizeLang(request.Language, "en");
        var encodedWord = Uri.EscapeDataString(word);
        var baseUrl = (_integrationOptions.FreeDictionaryBaseUrl ?? "https://api.dictionaryapi.dev").TrimEnd('/');
        var url = $"{baseUrl}/api/v2/entries/{language}/{encodedWord}";

        var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { error = "Word not found." });
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Dictionary lookup failed: {StatusCode} {Body}", response.StatusCode, body);
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Dictionary provider request failed." });
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<FreeDictionaryEntryDto>>(body, JsonOptions);
            var first = parsed?.FirstOrDefault();
            if (first is null)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "Dictionary returned empty payload." });
            }

            var result = new DictionaryLookupResponseDto
            {
                Provider = provider,
                Word = first.Word ?? word,
                Phonetic = first.Phonetic,
                Meanings = first.Meanings?
                    .Where(m => !string.IsNullOrWhiteSpace(m.PartOfSpeech))
                    .Select(m => new DictionaryMeaningDto
                    {
                        PartOfSpeech = m.PartOfSpeech ?? string.Empty,
                        Definitions = m.Definitions?
                            .Select(d => d.Definition ?? string.Empty)
                            .Where(d => !string.IsNullOrWhiteSpace(d))
                            .Take(3)
                            .ToList() ?? [],
                    })
                    .Where(m => m.Definitions.Count > 0)
                    .Take(8)
                    .ToList() ?? [],
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse dictionary response");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Dictionary response parse error." });
        }
    }

    private static string NormalizeLang(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
    }

    private sealed class MyMemoryResponseDto
    {
        public MyMemoryResponseDataDto? ResponseData { get; set; }
    }

    private sealed class MyMemoryResponseDataDto
    {
        public string? TranslatedText { get; set; }
    }

    private sealed class FreeDictionaryEntryDto
    {
        public string? Word { get; set; }

        public string? Phonetic { get; set; }

        public List<FreeDictionaryMeaningDto>? Meanings { get; set; }
    }

    private sealed class FreeDictionaryMeaningDto
    {
        public string? PartOfSpeech { get; set; }

        public List<FreeDictionaryDefinitionDto>? Definitions { get; set; }
    }

    private sealed class FreeDictionaryDefinitionDto
    {
        public string? Definition { get; set; }
    }
}

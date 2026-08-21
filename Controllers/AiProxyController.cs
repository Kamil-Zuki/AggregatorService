#nullable enable
using System.Text.Json;
using AggregatorService.Dtos;
using AggregatorService.Filters;
using AggregatorService.Options;
using AggregatorService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AggregatorService.Controllers;

/// <summary>
/// BFF-facing proxy for OpenAI-compatible LLMs (editor, reader mining). Secured via <see cref="AiProxyApiKeyFilter"/>.
/// </summary>
[ApiController]
[Route("api/ai")]
[AllowAnonymous]
[ServiceFilter(typeof(AiProxyApiKeyFilter))]
[AggregatorService.Filters.FeatureFlagFilter("EnableAIAgents")]
public class AiProxyController : ControllerBase
{
    private readonly OpenAiChatCompletionClient _chat;
    private readonly AiCompletionOptions _opts;
    private readonly ILogger<AiProxyController> _logger;

    private static readonly JsonSerializerOptions JsonParse = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public AiProxyController(
        OpenAiChatCompletionClient chat,
        IOptions<AiCompletionOptions> options,
        ILogger<AiProxyController> logger)
    {
        _chat = chat;
        _opts = options.Value;
        _logger = logger;
    }

    /// <summary>Configured models list for the editor UI.</summary>
    [HttpGet("models")]
    [ProducesResponseType(typeof(AiModelsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<AiModelsResponseDto> Models()
    {
        if (!_opts.Enabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "AI is disabled (Ai:Enabled=false)." });
        }

        if (string.IsNullOrWhiteSpace(_opts.ApiKey))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "AI API key is not configured (Ai:ApiKey)." });
        }

        var model = (_opts.Model ?? "").Trim();
        if (string.IsNullOrEmpty(model))
        {
            return BadRequest(new { error = "Ai:Model is not set on the Aggregator." });
        }

        if (_opts.AllowClientModelOverride)
        {
            return Ok(new AiModelsResponseDto
            {
                Models = [model],
                Provider = "openai-compatible",
            });
        }

        return Ok(new AiModelsResponseDto
        {
            Models = [model],
            Provider = "openai-compatible",
        });
    }

    /// <summary>Legacy text completion shape used by the card editor (plain prompt → plain text).</summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(AiGenerateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AiGenerateResponseDto>> Generate(
        [FromBody] AiProxyGenerateRequestDto? body,
        CancellationToken cancellationToken)
    {
        if (!_opts.Enabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "AI is disabled (Ai:Enabled=false)." });
        }

        if (string.IsNullOrWhiteSpace(_opts.ApiKey))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "AI API key is not configured (Ai:ApiKey)." });
        }

        var prompt = body?.Prompt?.Trim() ?? "";
        if (string.IsNullOrEmpty(prompt))
            return BadRequest(new { error = "Prompt is required" });

        if (body is { Stream: true })
            return BadRequest(new { error = "Stream is not supported; use stream=false." });

        var model = body?.Model?.Trim();
        const string system =
            "You are a helpful assistant. Answer with plain text only, no markdown fences, no preamble or labels unless the user asks.";

        try
        {
            var text = await _chat.CompleteAsync(model, system, prompt, cancellationToken).ConfigureAwait(false);
            var resolvedModel = _opts.AllowClientModelOverride && !string.IsNullOrEmpty(model)
                ? model!
                : (_opts.Model ?? "").Trim();

            return Ok(new AiGenerateResponseDto
            {
                Response = text.Trim(),
                Model = resolvedModel,
                Provider = "openai-compatible",
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI generate failed");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    /// <summary>Structured mining draft for the LingQ-style reader (contextual target + sentence translation).</summary>
    [HttpPost("mining-draft")]
    [ProducesResponseType(typeof(MiningDraftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MiningDraftResponseDto>> MiningDraft(
        [FromBody] MiningDraftRequestDto? body,
        CancellationToken cancellationToken)
    {
        if (!_opts.Enabled || string.IsNullOrWhiteSpace(_opts.ApiKey))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "AI completion is not available." });
        }

        var sentence = body?.Sentence?.Replace('\n', ' ').Trim() ?? "";
        var target = body?.Target?.Replace('\n', ' ').Trim() ?? "";
        if (string.IsNullOrEmpty(sentence) || string.IsNullOrEmpty(target))
            return BadRequest(new { error = "Sentence and target are required." });

        var src = (body?.SourceLanguage ?? "en").Trim();
        var tgt = (body?.TargetLanguage ?? "ru").Trim();

        var system =
            "You assist language learners. Output ONLY a single JSON object, no markdown, no extra text. "
            + "Keys: targetTranslationInContext (string, " + tgt + ", short, how the target is used in this sentence), "
            + "sentenceTranslation (string, full sentence in " + tgt + "), "
            + "dictionaryLemmaHint (string or null, optional dictionary headword in " + src + " — hint only, not identity). "
            + "Do not include explanations outside JSON.";

        var user = "Sentence (" + src + "): \"" + sentence + "\"\nTarget form as in text: \"" + target + "\"";

        try
        {
            var raw = await _chat.CompleteAsync(modelOverride: null, system, user, cancellationToken).ConfigureAwait(false);
            var jsonPayload = ExtractJsonObject(raw);
            var dto = JsonSerializer.Deserialize<MiningDraftResponseDto>(jsonPayload, JsonParse);
            if (dto == null)
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "Could not parse mining draft JSON." });

            dto.TargetTranslationInContext = dto.TargetTranslationInContext?.Trim() ?? "";
            dto.SentenceTranslation = dto.SentenceTranslation?.Trim() ?? "";
            dto.DictionaryLemmaHint = string.IsNullOrWhiteSpace(dto.DictionaryLemmaHint)
                ? null
                : dto.DictionaryLemmaHint.Trim();

            if (string.IsNullOrEmpty(dto.TargetTranslationInContext))
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { error = "LLM returned empty target translation." });
            }

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI mining-draft failed");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    private static string ExtractJsonObject(string text)
    {
        var t = text.Trim();
        var start = t.IndexOf("```", StringComparison.Ordinal);
        if (start >= 0)
        {
            var afterOpen = t.IndexOf('\n', start);
            if (afterOpen > 0)
            {
                afterOpen++;
                var end = t.LastIndexOf("```", StringComparison.Ordinal);
                if (end > afterOpen)
                    t = t[afterOpen..end].Trim();
            }
        }

        var i = t.IndexOf('{');
        var j = t.LastIndexOf('}');
        if (i >= 0 && j > i)
            return t[i..(j + 1)];
        return t;
    }

    public sealed class AiModelsResponseDto
    {
        public List<string> Models { get; set; } = [];

        public string Provider { get; set; } = "openai-compatible";
    }

    public sealed class AiGenerateResponseDto
    {
        public string Response { get; set; } = "";

        public string Model { get; set; } = "";

        public string Provider { get; set; } = "openai-compatible";
    }

    public sealed class AiProxyGenerateRequestDto
    {
        public string? Prompt { get; set; }

        public string? Model { get; set; }

        public bool Stream { get; set; }
    }
}

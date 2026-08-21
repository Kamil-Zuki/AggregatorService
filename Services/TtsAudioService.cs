#nullable enable
using AggregatorService.Dtos;
using AggregatorService.Options;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Pvs.Media.Grpc;

namespace AggregatorService.Services;

public sealed class TtsAudioService : ITtsAudioService
{
    private const int MaxInputLength = 4000;

    private static readonly HashSet<string> AllowedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "ru", "ko",
    };

    private readonly OpenAiSpeechClient _speech;
    private readonly IMediaServiceClient _media;
    private readonly IOptions<AiCompletionOptions> _options;
    private readonly ILogger<TtsAudioService> _logger;

    public TtsAudioService(
        OpenAiSpeechClient speech,
        IMediaServiceClient media,
        IOptions<AiCompletionOptions> options,
        ILogger<TtsAudioService> logger)
    {
        _speech = speech;
        _media = media;
        _options = options;
        _logger = logger;
    }

    public async Task<GenerateAudioResponseDto> GenerateAndStoreAsync(
        GenerateAudioRequestDto request,
        Guid userId,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var text = (request.Text ?? "").Trim();
        if (text.Length == 0)
            throw new ArgumentException("Text is required.", nameof(request));

        if (text.Length > MaxInputLength)
            throw new ArgumentException($"Text must not exceed {MaxInputLength} characters.", nameof(request));

        var lang = (request.Language ?? "en").Trim().ToLowerInvariant();
        if (!AllowedLanguages.Contains(lang))
            throw new ArgumentException("Language must be one of: en, ru, ko.", nameof(request));

        var o = _options.Value;
        var provider = TtsProviderHelper.ResolveProviderLabel(o);
        var model = TtsProviderHelper.ResolveTtsModel(o);
        var responseFormat = TtsProviderHelper.ResolveResponseFormat(o);

        var voice = TtsVoiceResolver.PickVoice(request.Voice, lang, o);
        var speed = request.Speed ?? o.TtsSpeed;
        speed = Math.Clamp(speed, 0.25, 4.0);

        _logger.LogInformation(
            "TTS generate: provider={Provider}, lang={Lang}, model={Model}, voice={Voice}",
            provider,
            lang,
            model,
            voice);

        var audioBytes = await _speech
            .CreateSpeechAsync(model, text, voice, responseFormat, speed, cancellationToken)
            .ConfigureAwait(false);

        var contentType = responseFormat.ToLowerInvariant() switch
        {
            "mp3" => "audio/mpeg",
            "opus" => "audio/opus",
            "aac" => "audio/aac",
            "flac" => "audio/flac",
            "wav" => "audio/wav",
            _ => "audio/mpeg",
        };

        var uploadRequest = new UploadAudioRequest
        {
            AudioData = ByteString.CopyFrom(audioBytes),
            ContentType = contentType,
        };

        var upload = await _media
            .UploadAudioAsync(uploadRequest, userId, roles, cancellationToken)
            .ConfigureAwait(false);

        return new GenerateAudioResponseDto
        {
            Url = upload.Url,
            AudioId = string.IsNullOrWhiteSpace(upload.AudioId) ? null : upload.AudioId,
            Provider = provider,
            Language = lang,
        };
    }
}

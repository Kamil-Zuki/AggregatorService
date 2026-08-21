#nullable enable

namespace AggregatorService.Options;

public enum TtsProvider
{
    OpenAi,
    Mistral,
    Espeak,
}

/// <summary>
/// Resolves TTS provider and defaults from <see cref="AiCompletionOptions.BaseUrl"/>.
/// </summary>
public static class TtsProviderHelper
{
    private static readonly HashSet<string> OpenAiVoiceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "alloy", "ash", "ballad", "coral", "echo", "fable", "nova", "onyx", "sage", "shimmer", "verse",
        "neutral_female",
    };

    public const string MistralDefaultModel = "voxtral-mini-tts-2603";
    public const string OpenAiDefaultModel = "tts-1";
    public const string EspeakModel = "espeak-ng";

    public static TtsProvider ResolveProvider(AiCompletionOptions options)
    {
        var configured = (options.TtsProvider ?? "auto").Trim().ToLowerInvariant();
        return configured switch
        {
            "" or "auto" => ResolveAutoProvider(options),
            "openai" or "openai-compatible" => TtsProvider.OpenAi,
            "mistral" => TtsProvider.Mistral,
            "espeak" or "espeak-ng" or "free" or "offline" => TtsProvider.Espeak,
            _ => throw new InvalidOperationException(
                "Unsupported TTS provider. Use AI_TTS_PROVIDER=auto, openai, mistral, or espeak."),
        };
    }

    public static bool IsMistralBaseUrl(string? baseUrl) =>
        !string.IsNullOrWhiteSpace(baseUrl) &&
        baseUrl.Contains("mistral.ai", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when a non-empty voice value is configured and is not an OpenAI-style name or placeholder.
    /// </summary>
    public static bool IsOpenAiStyleVoiceName(string? voice) =>
        string.IsNullOrWhiteSpace(voice) || OpenAiVoiceNames.Contains(voice.Trim());

    /// <summary>
    /// Mistral TTS needs a saved voice_id from the Mistral API, not OpenAI defaults like "alloy".
    /// </summary>
    public static bool HasValidMistralVoiceConfiguration(AiCompletionOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.TtsVoiceId) && !IsOpenAiStyleVoiceName(options.TtsVoiceId))
            return true;

        foreach (var candidate in new[] { options.TtsVoiceEn, options.TtsVoiceRu, options.TtsVoiceKo, options.TtsVoice })
        {
            if (!string.IsNullOrWhiteSpace(candidate) && !IsOpenAiStyleVoiceName(candidate))
                return true;
        }

        return false;
    }

    private static TtsProvider ResolveAutoProvider(AiCompletionOptions options)
    {
        if (!IsMistralBaseUrl(options.BaseUrl))
            return TtsProvider.OpenAi;

        return HasValidMistralVoiceConfiguration(options)
            ? TtsProvider.Mistral
            : TtsProvider.Espeak;
    }

    public static bool IsMistralProvider(AiCompletionOptions options) =>
        ResolveProvider(options) == TtsProvider.Mistral;

    public static bool IsEspeakProvider(AiCompletionOptions options) =>
        ResolveProvider(options) == TtsProvider.Espeak;

    public static string ResolveProviderLabel(AiCompletionOptions options) =>
        ResolveProvider(options) switch
        {
            TtsProvider.Mistral => "mistral",
            TtsProvider.Espeak => "espeak",
            _ => "openai-compatible",
        };

    public static string ResolveProviderLabel(string? baseUrl) =>
        IsMistralBaseUrl(baseUrl) ? "mistral" : "openai-compatible";

    /// <summary>
    /// Picks the outbound TTS model; avoids sending OpenAI-only model names to Mistral.
    /// </summary>
    public static string ResolveTtsModel(AiCompletionOptions options)
    {
        var configured = options.TtsModel?.Trim() ?? "";
        if (ResolveProvider(options) == TtsProvider.Espeak)
            return EspeakModel;

        if (ResolveProvider(options) == TtsProvider.Mistral)
        {
            if (string.IsNullOrEmpty(configured) ||
                configured.Equals(OpenAiDefaultModel, StringComparison.OrdinalIgnoreCase) ||
                configured.Equals("tts-1-hd", StringComparison.OrdinalIgnoreCase))
            {
                return MistralDefaultModel;
            }

            return configured;
        }

        return string.IsNullOrEmpty(configured) ? OpenAiDefaultModel : configured;
    }

    public static string ResolveResponseFormat(AiCompletionOptions options)
    {
        if (ResolveProvider(options) == TtsProvider.Espeak)
            return "wav";

        return string.IsNullOrWhiteSpace(options.TtsResponseFormat)
            ? "mp3"
            : options.TtsResponseFormat.Trim();
    }
}

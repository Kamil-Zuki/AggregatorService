#nullable enable
using AggregatorService.Options;

namespace AggregatorService.Services;

public static class TtsVoiceResolver
{
    public static string PickVoice(string? requestedVoice, string language, AiCompletionOptions options)
    {
        if (TtsProviderHelper.IsMistralProvider(options))
        {
            var mistralVoice = FirstNonEmpty(options.TtsVoiceId, requestedVoice, VoiceForLanguage(language, options), options.TtsVoice);
            if (string.IsNullOrWhiteSpace(mistralVoice) || TtsProviderHelper.IsOpenAiStyleVoiceName(mistralVoice))
            {
                throw new InvalidOperationException(
                    "Mistral TTS requires a saved voice_id. Create or list a Mistral voice and set AI_TTS_VOICE_ID.");
            }

            return mistralVoice.Trim();
        }

        if (TtsProviderHelper.IsEspeakProvider(options))
        {
            var voice = FirstEspeakVoice(requestedVoice, VoiceForLanguage(language, options));
            return string.IsNullOrEmpty(voice) ? DefaultEspeakVoice(language) : voice;
        }

        return FirstNonEmpty(requestedVoice, VoiceForLanguage(language, options), options.TtsVoice, "alloy");
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return "";
    }

    /// <summary>
    /// espeak-ng expects language voices (e.g. en-us, ru), not OpenAI names like alloy/nova from appsettings.
    /// </summary>
    private static string FirstEspeakVoice(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && !TtsProviderHelper.IsOpenAiStyleVoiceName(value))
                return value.Trim();
        }

        return "";
    }

    private static string? VoiceForLanguage(string language, AiCompletionOptions options)
    {
        return language.Trim().ToLowerInvariant() switch
        {
            "en" => options.TtsVoiceEn,
            "ru" => options.TtsVoiceRu,
            "ko" => options.TtsVoiceKo,
            _ => null,
        };
    }

    private static string DefaultEspeakVoice(string language)
    {
        return language.Trim().ToLowerInvariant() switch
        {
            "ru" => "ru",
            "ko" => "ko",
            _ => "en-us",
        };
    }
}

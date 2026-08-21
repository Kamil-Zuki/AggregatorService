namespace AggregatorService.Options;

/// <summary>
/// OpenAI-compatible chat completions (HTTPS + Bearer), shared by editor proxy, mining-draft, and Study Copilot.
/// </summary>
public class AiCompletionOptions
{
    /// <summary>Base URL without trailing slash, e.g. https://api.openai.com/v1</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>Bearer API key (server-side only).</summary>
    public string ApiKey { get; set; } = "";

    public string Model { get; set; } = "gpt-4o-mini";

    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>If false — proxy and copilot skip outbound LLM calls.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Shared secret for BFF → Aggregator (<c>/api/ai/*</c>): header <c>X-Ai-Proxy-Key</c>.
    /// </summary>
    public string ProxyApiKey { get; set; } = "";

    public bool AllowClientModelOverride { get; set; }

    /// <summary>Если false — <c>/api/Media/generate-audio</c> не вызывает внешний TTS.</summary>
    public bool TtsEnabled { get; set; } = true;

    /// <summary>auto, openai, mistral, or espeak. espeak is free/offline and does not require an API key.</summary>
    public string TtsProvider { get; set; } = "auto";

    /// <summary>TTS model (OpenAI: tts-1; Mistral: voxtral-mini-tts-2603). Auto-corrected when BaseUrl is Mistral.</summary>
    public string TtsModel { get; set; } = "tts-1";

    /// <summary>Mistral-only saved voice_id override. Takes precedence over per-request voices.</summary>
    public string? TtsVoiceId { get; set; }

    /// <summary>Default voice when no per-language override (OpenAI voice name or Mistral voice_id).</summary>
    public string TtsVoice { get; set; } = "alloy";

    public string TtsResponseFormat { get; set; } = "mp3";

    /// <summary>Default speech speed (0.25–4.0).</summary>
    public double TtsSpeed { get; set; } = 1.0;

    /// <summary>Optional default voices by study language (short code).</summary>
    public string? TtsVoiceEn { get; set; }

    public string? TtsVoiceRu { get; set; }

    public string? TtsVoiceKo { get; set; }

    /// <summary>Executable used by the free/offline espeak provider.</summary>
    public string TtsEspeakCommand { get; set; } = "espeak-ng";
}

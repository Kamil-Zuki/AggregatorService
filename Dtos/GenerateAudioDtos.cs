#nullable enable

namespace AggregatorService.Dtos;

/// <summary>
/// Server-side TTS: синтез речи и загрузка в MediaService.
/// </summary>
public class GenerateAudioRequestDto
{
    /// <summary>Текст для озвучки (trim).</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Язык изучаемого контента: en, ru, ko.</summary>
    public string Language { get; set; } = "en";

    /// <summary>Опционально: OpenAI voice name or Mistral saved voice_id.</summary>
    public string? Voice { get; set; }

    /// <summary>Опционально: скорость 0.25–4.0.</summary>
    public double? Speed { get; set; }
}

public class GenerateAudioResponseDto
{
    public string Url { get; set; } = string.Empty;

    public string? AudioId { get; set; }

    public string Provider { get; set; } = "openai-compatible";

    public string Language { get; set; } = "en";
}

namespace AggregatorService.Dtos;

/// <summary>
/// DTO для обновления проекта
/// </summary>
public class UpdateProjectDto
{
    /// <summary>
    /// Название проекта (опционально)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Флаг архивации (опционально)
    /// </summary>
    public bool? IsArchived { get; set; }

    /// <summary>
    /// Настройки FSRS (опционально)
    /// </summary>
    public SrsSettingsDto? Settings { get; set; }

    /// <summary>
    /// Настройки TTS (опционально)
    /// </summary>
    public TtsSettingsDto? TtsSettings { get; set; }
}


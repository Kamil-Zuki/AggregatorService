namespace AggregatorService.Dtos;

/// <summary>
/// DTO для ответа с данными проекта
/// </summary>
public class ProjectResponseDto
{
    /// <summary>
    /// Идентификатор проекта
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Название проекта
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Код родного языка
    /// </summary>
    public string SourceLang { get; set; } = string.Empty;

    /// <summary>
    /// Код изучаемого языка
    /// </summary>
    public string TargetLang { get; set; } = string.Empty;

    /// <summary>
    /// Настройки SRS
    /// </summary>
    public SrsSettingsDto? Settings { get; set; }

    /// <summary>
    /// Настройки TTS
    /// </summary>
    public TtsSettingsDto? TtsSettings { get; set; }

    /// <summary>
    /// Статистика проекта
    /// </summary>
    public ProjectStatsDto? Stats { get; set; }

    /// <summary>
    /// Флаг архивного проекта
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO для статистики проекта
/// </summary>
public class ProjectStatsDto
{
    /// <summary>
    /// Общее количество лемм
    /// </summary>
    public int TotalTerms { get; set; }

    /// <summary>
    /// Количество зрелых лемм
    /// </summary>
    public int KnownTerms { get; set; }
}


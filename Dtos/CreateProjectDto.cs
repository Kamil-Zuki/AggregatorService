using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos;

/// <summary>
/// DTO для создания проекта через REST API
/// </summary>
public class CreateProjectDto
{
    /// <summary>
    /// Название проекта
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Код родного языка (ISO 639-1)
    /// </summary>
    [Required]
    [StringLength(2)]
    public string SourceLang { get; set; } = string.Empty;

    /// <summary>
    /// Код изучаемого языка (ISO 639-1)
    /// </summary>
    [Required]
    [StringLength(2)]
    public string TargetLang { get; set; } = string.Empty;

    /// <summary>
    /// Настройки SRS (опционально)
    /// </summary>
    public SrsSettingsDto? Settings { get; set; }

    /// <summary>
    /// Настройки TTS (опционально)
    /// </summary>
    public TtsSettingsDto? TtsSettings { get; set; }
}

/// <summary>
/// DTO для настроек TTS браузера
/// </summary>
public class TtsSettingsDto
{
    /// <summary>
    /// Имя голоса браузера (например "Google US English")
    /// </summary>
    public string? VoiceName { get; set; }

    /// <summary>
    /// Скорость речи (0.1 - 10.0)
    /// </summary>
    [Range(0.1, 10.0)]
    public double Rate { get; set; } = 1.0;

    /// <summary>
    /// Высота голоса (0.0 - 2.0)
    /// </summary>
    [Range(0.0, 2.0)]
    public double Pitch { get; set; } = 1.0;
}

/// <summary>
/// DTO для настроек SRS
/// </summary>
public class SrsSettingsDto
{
    /// <summary>
    /// Целевая доля запоминания (0.0 - 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public double RequestRetention { get; set; } = 0.9;

    /// <summary>
    /// Максимальный интервал повторения в днях
    /// </summary>
    [Range(1, 36500)]
    public int MaximumInterval { get; set; } = 36500;

    /// <summary>
    /// Веса FSRS (18 значений)
    /// </summary>
    public double[]? W { get; set; }

    /// <summary>
    /// Включить краткосрочные повторения
    /// </summary>
    public bool EnableShortTerm { get; set; } = true;
}


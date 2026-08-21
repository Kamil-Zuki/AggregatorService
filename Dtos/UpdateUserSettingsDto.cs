using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos;

/// <summary>
/// DTO для обновления настроек пользователя через REST API
/// </summary>
public class UpdateUserSettingsDto
{
    /// <summary>
    /// Час начала нового дня (0-23) (опционально)
    /// </summary>
    [Range(0, 23)]
    public int? RolloverHour { get; set; }

    /// <summary>
    /// Дневная цель новых карточек (опционально)
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? DailyGoalNew { get; set; }

    /// <summary>
    /// Дневная цель повторений (опционально)
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? DailyGoalReview { get; set; }

    /// <summary>
    /// Язык интерфейса (опционально)
    /// </summary>
    [StringLength(10)]
    public string? InterfaceLanguage { get; set; }
}


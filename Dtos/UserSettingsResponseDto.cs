namespace AggregatorService.Dtos;

/// <summary>
/// DTO для ответа с настройками пользователя
/// </summary>
public class UserSettingsResponseDto
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Час начала нового дня (0-23)
    /// </summary>
    public int RolloverHour { get; set; }

    /// <summary>
    /// Дневная цель новых карточек
    /// </summary>
    public int DailyGoalNew { get; set; }

    /// <summary>
    /// Дневная цель повторений
    /// </summary>
    public int DailyGoalReview { get; set; }

    /// <summary>
    /// Язык интерфейса
    /// </summary>
    public string InterfaceLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Текущая серия дней
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Максимальная серия дней
    /// </summary>
    public int MaxStreak { get; set; }
}


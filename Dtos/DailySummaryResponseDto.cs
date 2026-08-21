namespace AggregatorService.Dtos;

/// <summary>
/// DTO для ответа с дневной сводкой
/// </summary>
public class DailySummaryResponseDto
{
    /// <summary>
    /// Дата (YYYY-MM-DD)
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Текущая серия дней
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Была ли серия продлена сегодня
    /// </summary>
    public bool IsStreakExtendedToday { get; set; }

    /// <summary>
    /// Время, потраченное на обучение (в секундах)
    /// </summary>
    public int TimeSpentSeconds { get; set; }

    /// <summary>
    /// Статистика по новым карточкам
    /// </summary>
    public GoalProgressDto NewCards { get; set; } = new();

    /// <summary>
    /// Статистика по повторениям
    /// </summary>
    public GoalProgressDto Reviews { get; set; } = new();
}

/// <summary>
/// DTO для прогресса по цели
/// </summary>
public class GoalProgressDto
{
    /// <summary>
    /// Текущее значение
    /// </summary>
    public int Current { get; set; }

    /// <summary>
    /// Целевое значение
    /// </summary>
    public int Target { get; set; }

    /// <summary>
    /// Достигнута ли цель
    /// </summary>
    public bool IsCompleted { get; set; }
}

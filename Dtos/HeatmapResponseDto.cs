namespace AggregatorService.Dtos;

/// <summary>
/// DTO для ответа с данными календаря активности
/// </summary>
public class HeatmapResponseDto
{
    /// <summary>
    /// Идентификатор проекта (null если для всех проектов)
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Год
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Общее количество повторений
    /// </summary>
    public int TotalReviews { get; set; }

    /// <summary>
    /// Самая длинная серия
    /// </summary>
    public int LongestStreak { get; set; }

    /// <summary>
    /// Сумма времени изучения за год (секунды)
    /// </summary>
    public int TotalTimeSpentSeconds { get; set; }

    /// <summary>
    /// Активность по дням (ключ: дата в формате YYYY-MM-DD)
    /// </summary>
    public Dictionary<string, ActivityDayDto> Activity { get; set; } = new();
}

/// <summary>
/// DTO для активности за день
/// </summary>
public class ActivityDayDto
{
    /// <summary>
    /// Количество повторений
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Уровень интенсивности (1-4)
    /// </summary>
    public int Level { get; set; }
}

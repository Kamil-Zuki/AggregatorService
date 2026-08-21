namespace AggregatorService.Dtos;

/// <summary>
/// DTO для ответа со статистикой словарного запаса
/// </summary>
public class VocabularyStatsResponseDto
{
    /// <summary>
    /// Идентификатор проекта
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Общее количество лемм
    /// </summary>
    public int TotalTerms { get; set; }

    /// <summary>
    /// Количество зрелых лемм
    /// </summary>
    public int MatureCount { get; set; }

    /// <summary>
    /// Saved terms without linked FSRS cards.
    /// </summary>
    public int SavedCount { get; set; }

    /// <summary>
    /// Terms with non-mature linked FSRS cards.
    /// </summary>
    public int ReviewingCount { get; set; }

    /// <summary>
    /// Active learning total (Saved + In Review).
    /// </summary>
    public int LearningCount { get; set; }

    /// <summary>
    /// Количество новых лемм
    /// </summary>
    public int NewCount { get; set; }

    /// <summary>
    /// Уровень CEFR
    /// </summary>
    public CefrLevelDto? CefrLevel { get; set; }

    /// <summary>
    /// Оценка беглости (0-100)
    /// </summary>
    public int EstimatedFluency { get; set; }
}

/// <summary>
/// DTO для уровня CEFR
/// </summary>
public class CefrLevelDto
{
    /// <summary>
    /// Код уровня (A1, A2, B1, B2, C1, C2)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Название уровня
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Прогресс в процентах (0-100)
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// Количество слов до следующего уровня
    /// </summary>
    public int WordsToNextLevel { get; set; }
}

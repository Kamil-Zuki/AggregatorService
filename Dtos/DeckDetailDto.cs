namespace AggregatorService.Dtos;

/// <summary>
/// DTO детальной информации о колоде (GET /api/decks/{id})
/// </summary>
public class DeckDetailDto
{
    /// <summary>
    /// Идентификатор колоды
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Название колоды
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание колоды
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Идентификатор родительской колоды (для хлебных крошек)
    /// </summary>
    public string? ParentDeckId { get; set; }

    /// <summary>
    /// Идентификатор проекта
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор владельца
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// URL обложки колоды
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Публичная колода
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Политика вкладов
    /// </summary>
    public ContributionPolicyDto ContributionPolicy { get; set; }

    /// <summary>
    /// Тип лицензии
    /// </summary>
    public LicenseTypeDto LicenseType { get; set; }

    /// <summary>
    /// Идентификатор колоды-источника, если скачано/куплено
    /// </summary>
    public string? ForkedFromId { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Количество карточек в колоде
    /// </summary>
    public int CardCount { get; set; }

    /// <summary>
    /// Статистика по карточкам колоды
    /// </summary>
    public DeckDetailStatsDto Stats { get; set; } = new();
}

/// <summary>
/// Статистика карточек в колоде
/// </summary>
public class DeckDetailStatsDto
{
    public int NewCardsCount { get; set; }
    public int LearningCardsCount { get; set; }
    public int DueCardsCount { get; set; }
    public int StudyableNowCount { get; set; }
    public int TotalCardsCount { get; set; }
}

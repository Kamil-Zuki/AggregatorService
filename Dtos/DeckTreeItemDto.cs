namespace AggregatorService.Dtos;

/// <summary>
/// DTO для элемента дерева колод
/// </summary>
public class DeckTreeItemDto
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
    /// Количество карточек в колоде
    /// </summary>
    public int CardCount { get; set; }

    /// <summary>
    /// Дочерние колоды (рекурсивная структура)
    /// </summary>
    public List<DeckTreeItemDto> Children { get; set; } = new();

    /// <summary>
    /// Идентификатор владельца (для фильтра «Мои»).
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Публичная колода (для фильтра «Публичные»).
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Идентификатор колоды-источника, если скачано/куплено (для фильтра «Скачанные» и бейджа Purchased).
    /// </summary>
    public string? ForkedFromId { get; set; }

    /// <summary>
    /// URL обложки колоды.
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Статистика карточек для текущего пользователя.
    /// </summary>
    public DeckDetailStatsDto Stats { get; set; } = new();
}


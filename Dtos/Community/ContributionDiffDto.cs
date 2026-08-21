namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для различий в предложении (SR-COL-03)
/// </summary>
public class ContributionDiffDto
{
    /// <summary>
    /// Оригинальная карточка (null для ADD)
    /// </summary>
    public CardContentDto? OriginalCard { get; set; }

    /// <summary>
    /// Предложенная карточка
    /// </summary>
    public CardContentDto ProposedCard { get; set; } = new();

    /// <summary>
    /// Список измененных полей
    /// </summary>
    public List<string> ChangedFields { get; set; } = new();

    /// <summary>
    /// Есть ли конфликт версий
    /// </summary>
    public bool IsConflict { get; set; }
}

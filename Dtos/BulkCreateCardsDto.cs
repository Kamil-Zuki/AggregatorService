namespace AggregatorService.Dtos;

/// <summary>
/// DTO для массового создания карточек
/// </summary>
public class BulkCreateCardsDto
{
    /// <summary>
    /// Идентификатор колоды
    /// </summary>
    public string DeckId { get; set; } = string.Empty;

    /// <summary>
    /// Список карточек для создания
    /// </summary>
    public List<CreateCardDto> Cards { get; set; } = new();
}

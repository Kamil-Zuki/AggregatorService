namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для публикации колоды (SR-PUB-01)
/// </summary>
public class PublishDeckDto
{
    /// <summary>
    /// ID колоды
    /// </summary>
    public Guid DeckId { get; set; }

    /// <summary>
    /// Тип лицензии: FREE или COMMERCIAL
    /// </summary>
    public string LicenseType { get; set; } = "FREE";
}

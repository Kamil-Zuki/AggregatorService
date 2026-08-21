namespace AggregatorService.Dtos;

/// <summary>
/// Response DTO for deck subscription (list item and subscribe response).
/// Aligns with frontend DeckSubscriptionDto: id, userId, deckId, lastSyncedVersion, subscribedAt, lastAccessedAt, deckTitle.
/// </summary>
public class DeckSubscriptionDto
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string DeckId { get; set; } = null!;
    public int LastSyncedVersion { get; set; }
    public DateTime SubscribedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public string? DeckTitle { get; set; }
}

namespace AggregatorService.Dtos.Subscriptions;

/// <summary>
/// DTO for a single subscription list item from VocabularyService (PVS).
/// </summary>
public class SubscriptionListItemDto
{
    public Guid DeckId { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime SubscribedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int LastSyncedVersion { get; set; }
}

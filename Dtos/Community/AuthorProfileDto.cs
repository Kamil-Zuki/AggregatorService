namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для профиля автора (SR-PUB-04)
/// </summary>
public class AuthorProfileDto
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public int PublishedDecksCount { get; set; }
    public int TotalForksCount { get; set; }
    public double? AverageRating { get; set; }
    public DateTime JoinedAt { get; set; }
}

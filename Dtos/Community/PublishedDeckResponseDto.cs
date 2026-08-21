namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для опубликованной колоды
/// </summary>
public class PublishedDeckResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public AuthorInfoDto Author { get; set; } = new();
    public string LicenseType { get; set; } = "FREE";
    public int CardCount { get; set; }
    public double? Rating { get; set; }
    public int ReviewCount { get; set; }
    public int ForkCount { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

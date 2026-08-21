namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для товара
/// </summary>
public class ProductResponseDto
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? DescriptionHtml { get; set; }
    public string? CoverImageUrl { get; set; }
    public AuthorInfoDto Author { get; set; } = new();
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty; // PENDING_REVIEW, PUBLISHED, ARCHIVED
    public double? Rating { get; set; }
    public int ReviewCount { get; set; }
    public int PurchaseCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

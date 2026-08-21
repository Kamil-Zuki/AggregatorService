namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для обновления товара (SR-MKT-01)
/// </summary>
public class UpdateProductDto
{
    public string? Title { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
}

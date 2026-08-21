namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для статистики товара (SR-MKT-06)
/// </summary>
public class ProductStatsDto
{
    public int TotalPurchases { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int RefundCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

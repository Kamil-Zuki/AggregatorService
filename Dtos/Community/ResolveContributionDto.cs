namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для принятия/отклонения предложения (SR-COL-04)
/// </summary>
public class ResolveContributionDto
{
    /// <summary>
    /// Статус: MERGED или REJECTED
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Комментарий к решению
    /// </summary>
    public string? ResolutionComment { get; set; }
}

namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для обновления политики вкладов (SR-COL-06)
/// </summary>
public class UpdateContributionPolicyDto
{
    /// <summary>
    /// Политика: OPEN, RESTRICTED, CLOSED
    /// </summary>
    public string Policy { get; set; } = string.Empty;
}

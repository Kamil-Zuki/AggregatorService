namespace AggregatorService.Dtos;

/// <summary>
/// DTO для запроса старта сессии обучения (SR-LRN-01)
/// </summary>
public class StartSessionRequestDto
{
    public string ProjectId { get; set; } = string.Empty;
    public string? DeckId { get; set; }
    public string? Mode { get; set; } // STANDARD, etc.
}

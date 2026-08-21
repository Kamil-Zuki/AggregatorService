namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для клонирования колоды (SR-PUB-02)
/// </summary>
public class ForkDeckDto
{
    /// <summary>
    /// ID колоды для клонирования
    /// </summary>
    public Guid DeckId { get; set; }

    /// <summary>
    /// ID целевого проекта
    /// </summary>
    public Guid TargetProjectId { get; set; }

    /// <summary>
    /// Новое название колоды (опционально)
    /// </summary>
    public string? NewTitle { get; set; }
}

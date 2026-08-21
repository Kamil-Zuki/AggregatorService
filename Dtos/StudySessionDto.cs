namespace AggregatorService.Dtos;

/// <summary>
/// DTO для представления сессии обучения
/// </summary>
public class StudySessionDto
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, COMPLETED
    public DateTime StartTime { get; set; }
    public int CardsReviewed { get; set; }
    public QueueStatsDto QueueStats { get; set; } = new();
}

/// <summary>
/// Статистика очереди карточек
/// </summary>
public class QueueStatsDto
{
    public int New { get; set; }
    public int Review { get; set; }
    public int Learning { get; set; }
}

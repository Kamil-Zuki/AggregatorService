namespace AggregatorService.Dtos;

public class SrsStateDto
{
    public string State { get; set; } = "NEW";
    public int CurrentInterval { get; set; }
    public int Step { get; set; }
    public DateTime? DueUtc { get; set; }
    public int Lapses { get; set; }
    public double Stability { get; set; }
    public double Difficulty { get; set; }
    public int ScheduledDays { get; set; }
    public int ElapsedDays { get; set; }
}

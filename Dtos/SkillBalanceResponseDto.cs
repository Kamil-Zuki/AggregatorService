namespace AggregatorService.Dtos;

public class SkillBalanceResponseDto
{
    public string ProjectId { get; set; } = string.Empty;
    public int AverageReadingLevel { get; set; }
    public int AverageListeningLevel { get; set; }
    public int AverageWritingLevel { get; set; }
    public int AverageSpeakingLevel { get; set; }
}

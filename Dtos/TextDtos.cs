namespace AggregatorService.Dtos;

public class TextAnalyzeRequestDto
{
    public string ProjectId { get; set; } = null!;

    public string Text { get; set; } = null!;
}

public class TextAnalyzeResponseDto
{
    public List<TextTokenDto> Tokens { get; set; } = [];

    public TextAnalysisStatsDto Stats { get; set; } = new();
}

public class TextTokenDto
{
    public string Text { get; set; } = null!;

    public string? TermText { get; set; }

    public string Status { get; set; } = "NONE";

    public string Type { get; set; } = "WORD";
}

public class TextAnalysisStatsDto
{
    public int UniqueWords { get; set; }

    public double KnownPercentage { get; set; }

    public int NewWordsCount { get; set; }

    public int LearningWordsCount { get; set; }

    public int KnownWordsCount { get; set; }
}

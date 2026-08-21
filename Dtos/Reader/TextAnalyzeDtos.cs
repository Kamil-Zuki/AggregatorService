namespace AggregatorService.Dtos.Reader;

public class TextAnalyzeRequestDto
{
    public required string ProjectId { get; set; }
    public required string Text { get; set; }
}

public class TextTokenDto
{
    public required string Text { get; set; }
    public string? TermText { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? ProjectTermId { get; set; }
}

public class TextPhraseDto
{
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public required string Text { get; set; }
    public string? Status { get; set; }
    public string? ProjectTermId { get; set; }
}

public class TextAnalyzeStatsDto
{
    public int UniqueWords { get; set; }
    public double KnownPercentage { get; set; }
    public int? NewWordsCount { get; set; }
    public int? LearningWordsCount { get; set; }
    public int? KnownWordsCount { get; set; }
}

public class TextAnalyzeResponseDto
{
    public List<TextTokenDto> Tokens { get; set; } = [];
    public List<TextPhraseDto> Phrases { get; set; } = [];
    public TextAnalyzeStatsDto Stats { get; set; } = new();
}

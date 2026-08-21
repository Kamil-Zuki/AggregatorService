namespace AggregatorService.Dtos;

public class CreateOrUpdateTermDto
{
    public string ProjectId { get; set; } = null!;
    public string TermText { get; set; } = null!;
    public string Type { get; set; } = "WORD";
    public string Language { get; set; } = "";
    public string Status { get; set; } = "SAVED";
    public string? Meaning { get; set; }
    public string? FirstSentence { get; set; }
    public string? FirstSourceTitle { get; set; }
    public string? FirstSourceUrl { get; set; }
}

public class TermActionDto
{
    public string ProjectId { get; set; } = null!;
    public string TermText { get; set; } = null!;
    public string Type { get; set; } = "WORD";
    public string Language { get; set; } = "";
}

public class BulkMarkKnownItemDto
{
    public string TermText { get; set; } = null!;
    public string Type { get; set; } = "WORD";
}

public class BulkMarkKnownDto
{
    public string ProjectId { get; set; } = null!;
    public List<string> TermTexts { get; set; } = [];
    public List<BulkMarkKnownItemDto> Items { get; set; } = [];
    public string Language { get; set; } = "";
}

public class BulkMarkKnownResponseDto
{
    public int UpdatedCount { get; set; }
}

public class TermDetailsDto
{
    public string TermId { get; set; } = null!;
    public string ProjectId { get; set; } = null!;
    public string TermText { get; set; } = null!;
    public string NormalizedText { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Language { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Meaning { get; set; }
    public string? FirstSentence { get; set; }
    public string? FirstSourceTitle { get; set; }
    public string? FirstSourceUrl { get; set; }
    public List<CardPreviewDto> RelatedCards { get; set; } = [];
    public int ReadingLevel { get; set; }
    public int ListeningLevel { get; set; }
    public int WritingLevel { get; set; }
    public int SpeakingLevel { get; set; }
}

public class SearchTermDuplicatesDto
{
    public string ProjectId { get; set; } = null!;
    public string TermText { get; set; } = null!;
    public string Type { get; set; } = "WORD";
}

public class SearchTermDuplicatesResponseDto
{
    public bool IsDuplicate { get; set; }
    public string NormalizedText { get; set; } = null!;
    public List<TermDetailsDto> MatchingTerms { get; set; } = [];
    public List<CardPreviewDto> ExistingCards { get; set; } = [];
}

public class ProjectTermListItemDto
{
    public string TermId { get; set; } = null!;
    public string Text { get; set; } = null!;
    public string NormalizedText { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Language { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Meaning { get; set; }
    public string? FirstSentence { get; set; }
    public string? FirstSourceTitle { get; set; }
    public string? FirstSourceUrl { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int RelatedCardCount { get; set; }
    public int ReadingLevel { get; set; }
    public int ListeningLevel { get; set; }
    public int WritingLevel { get; set; }
    public int SpeakingLevel { get; set; }
}

public class ListProjectTermsResponseDto
{
    public List<ProjectTermListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class PurgeDemoImportResponseDto
{
    public int CardsDeleted { get; set; }
    public int StatusesDeleted { get; set; }
    public int TermsDeleted { get; set; }
}

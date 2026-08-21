namespace AggregatorService.Dtos;

public class CheckCardDuplicatesRequestDto
{
    public string ProjectId { get; set; } = string.Empty;
    public string TermText { get; set; } = string.Empty;
}

public class CheckCardDuplicatesResponseDto
{
    public bool IsDuplicate { get; set; }
    public string? NormalizedSurface { get; set; }
    public List<CardPreviewDto> ExistingCards { get; set; } = [];
}

public class CardPreviewDto
{
    public string Id { get; set; } = string.Empty;
    public NotePayloadDto? Note { get; set; }
    public string? SrsStatus { get; set; }
    public bool? HasAudio { get; set; }
    public string? DeckTitle { get; set; }
}

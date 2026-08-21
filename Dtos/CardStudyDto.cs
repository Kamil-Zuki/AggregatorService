namespace AggregatorService.Dtos;

/// <summary>
/// Карточка в режиме обучения. Контент — поля заметки + вычисленный target index.
/// </summary>
public class CardStudyDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "SENTENCE_MINING";
    public CardStudyContentDto Content { get; set; } = new();
    public SourceMetaDto? SourceMeta { get; set; }
    public CardMediaDto? Media { get; set; }
    public SrsStateDto SrsState { get; set; } = new();
    public Dictionary<int, string> NextIntervals { get; set; } = new();
    public int SiblingsCount { get; set; }
}

public class CardStudyContentDto
{
    public NotePayloadDto Note { get; set; } = new();
    public TargetIndexDto TargetIndex { get; set; } = new();
}


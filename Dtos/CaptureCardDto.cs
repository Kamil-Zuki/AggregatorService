namespace AggregatorService.Dtos;

/// <summary>Захват карточки: поля заметки + опциональный скриншот (заливается в Image).</summary>
public class CaptureCardDto
{
    public string ProjectId { get; set; } = string.Empty;

    public Dictionary<string, NoteFieldValueDto> FieldValues { get; set; } = new();

    public string? ScreenshotBase64 { get; set; }

    /// <summary>Optional deck UUID; when omitted, VocabularyService uses the project Inbox deck.</summary>
    public string? DeckId { get; set; }
}

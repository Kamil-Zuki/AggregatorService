namespace AggregatorService.Dtos;

/// <summary>DTO для создания карточки: только поля заметки (Sentence Mining keys и др.).</summary>
public class CreateCardDto
{
    /// <summary>Идентификатор колоды (UUID).</summary>
    public string DeckId { get; set; } = string.Empty;

    /// <summary>Anki-like map: Expression, Word, Translation, Image, Audio, …</summary>
    public Dictionary<string, NoteFieldValueDto> FieldValues { get; set; } = new();
}

namespace AggregatorService.Dtos;

/// <summary>Частичное обновление карточки через map полей заметки.</summary>
public class UpdateCardDto
{
    public Dictionary<string, NoteFieldValueDto> FieldValues { get; set; } = new();
}

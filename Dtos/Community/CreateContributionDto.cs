using System.Collections.Generic;
using AggregatorService.Dtos;

namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для создания предложения (SR-COL-01)
/// </summary>
public class CreateContributionDto
{
    /// <summary>
    /// ID колоды
    /// </summary>
    public Guid DeckId { get; set; }

    /// <summary>
    /// ID карточки (обязательно для EDIT/DELETE)
    /// </summary>
    public Guid? CardId { get; set; }

    /// <summary>
    /// Тип предложения: EDIT, ADD, DELETE
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Содержимое карточки (обязательно для EDIT/ADD)
    /// </summary>
    public CardContentDto Content { get; set; } = new();

    /// <summary>
    /// Комментарий к предложению
    /// </summary>
    public string? Comment { get; set; }
}

/// <summary>Содержимое предложения: только map полей заметки.</summary>
public class CardContentDto
{
    public Dictionary<string, NoteFieldValueDto> FieldValues { get; set; } = new();
}

namespace AggregatorService.Dtos;

/// <summary>Карточка: идентичность + SRS; контент только в <see cref="Note"/>.</summary>
public class CardResponseDto
{
    public string Id { get; set; } = string.Empty;

    public string DeckId { get; set; } = string.Empty;

    public string CreatorId { get; set; } = string.Empty;

    public NotePayloadDto? Note { get; set; }

    public CardTemplateDto? ActiveCardTemplate { get; set; }

    public string? ProjectTermId { get; set; }

    public string SrsStatus { get; set; } = string.Empty;

    public SrsStateDto? SrsState { get; set; }

    public DateTime CreatedAt { get; set; }
}

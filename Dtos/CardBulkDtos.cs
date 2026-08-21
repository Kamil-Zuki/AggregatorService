using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos;

public class BulkCardIdsRequestDto
{
    [Required]
    public List<string> CardIds { get; set; } = [];
}

public class MoveCardsRequestDto
{
    [Required]
    public List<string> CardIds { get; set; } = [];

    [Required]
    public string DeckId { get; set; } = string.Empty;
}

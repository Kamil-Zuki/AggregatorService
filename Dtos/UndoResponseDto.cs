namespace AggregatorService.Dtos;

/// <summary>
/// DTO для ответа на отмену действия
/// </summary>
public class UndoResponseDto
{
    public bool Success { get; set; }
    public string RestoredCardId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

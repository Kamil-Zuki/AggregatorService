namespace AggregatorService.Dtos;

public class ReaderCollectionCollaboratorDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
    public string SharedAt { get; set; } = string.Empty;
}

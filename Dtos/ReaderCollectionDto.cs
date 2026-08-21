namespace AggregatorService.Dtos;

public class ReaderCollectionDto
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string OwnerUserName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public bool IsSharedWithMe { get; set; }
    public bool CanEdit { get; set; }
    public int BookCount { get; set; }
    public List<ReaderCollectionCollaboratorDto> Collaborators { get; set; } = [];
    public List<ReaderLibraryBookDto> Books { get; set; } = [];
}

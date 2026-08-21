namespace AggregatorService.Dtos;

public class ReaderLibraryBookDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
    public int? PageCount { get; set; }
    public string UploadedAt { get; set; } = string.Empty;
    public string? LastOpenedAt { get; set; }
    public int? LastReadPage { get; set; }
    public string? CollectionId { get; set; }
    public string? CollectionName { get; set; }
    public bool IsShared { get; set; }
    public string? OwnerUserId { get; set; }
    public string? OwnerUserName { get; set; }
    public string? OwnerEmail { get; set; }
    /// <summary>pdf | extracted</summary>
    public string ReadingMode { get; set; } = "pdf";
    public bool HasExtractedText { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? CefrLevel { get; set; }
    public string? Summary { get; set; }
}

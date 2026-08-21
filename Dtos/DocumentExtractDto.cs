namespace AggregatorService.Dtos;

/// <summary>
/// Sidecar JSON stored next to a Reader document (OCR / extracted text pages).
/// </summary>
public sealed class DocumentExtractDto
{
    public required string SourceFormat { get; init; }

    public string? Language { get; init; }

    public string? Warning { get; init; }

    public IReadOnlyList<DocumentExtractPageDto> Pages { get; init; } = [];
}

public sealed class DocumentExtractPageDto
{
    public int PageNumber { get; init; }

    public required string Text { get; init; }
}

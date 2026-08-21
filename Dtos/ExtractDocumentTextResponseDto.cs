namespace AggregatorService.Dtos;

/// <summary>
/// Plain text extracted server-side from PDF / EPUB / TXT for the reader pipeline (paste still uses <c>/api/text/analyze</c> directly).
/// </summary>
public sealed class ExtractDocumentTextResponseDto
{
    public required string Text { get; init; }

    public string? Title { get; init; }

    /// <summary>pdf | pdf+ocr | epub | txt</summary>
    public required string SourceFormat { get; init; }

    public bool UsedOcr { get; init; }

    /// <summary>Optional machine-readable warning (e.g. OCR_PAGE_LIMIT).</summary>
    public string? Warning { get; init; }

    public IReadOnlyList<ExtractDocumentTextPageDto> Pages { get; init; } = [];
}

public sealed class ExtractDocumentTextPageDto
{
    public int PageNumber { get; init; }

    public required string Text { get; init; }
}

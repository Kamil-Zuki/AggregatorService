namespace AggregatorService.Services;

public interface IDocumentTextExtractor
{
    /// <summary>
    /// Reads plain text from PDF (text layer + OCR fallback), EPUB (XHTML chapters), or UTF-8/UTF-16 TXT.
    /// </summary>
    /// <exception cref="NoExtractableDocumentTextException">Document parsed but no meaningful text.</exception>
    Task<ExtractDocumentTextResult> ExtractAsync(
        Stream stream,
        string fileName,
        string? contentType,
        string? language = null,
        CancellationToken cancellationToken = default);
}

public sealed record ExtractDocumentTextPage(int PageNumber, string Text);

public sealed record ExtractDocumentTextResult(
    string Text,
    string? Title,
    string SourceFormat,
    IReadOnlyList<ExtractDocumentTextPage> Pages,
    bool UsedOcr,
    string? Warning = null);

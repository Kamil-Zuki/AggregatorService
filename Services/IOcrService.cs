namespace AggregatorService.Services;

public sealed record OcrPageResult(int PageNumber, string Text);

public sealed record OcrRecognizeResult(
    string? Text,
    IReadOnlyList<OcrPageResult> Pages,
    int PageCount,
    string? Warning);

public interface IOcrService
{
    /// <summary>
    /// Runs OCR over a PDF document and returns per-page text (or empty pages if nothing found).
    /// </summary>
    Task<OcrRecognizeResult> RecognizePdfAsync(
        byte[] pdfBytes,
        string? language = null,
        CancellationToken cancellationToken = default);
}

namespace AggregatorService.Services;

/// <summary>
/// Raised when a document opens successfully but yields no usable plain text (e.g. scanned PDF).
/// Maps to HTTP 422 with a structured body for the reader UI.
/// </summary>
public sealed class NoExtractableDocumentTextException : Exception
{
    public NoExtractableDocumentTextException(string sourceFormat, string reasonCode, string message)
        : base(message)
    {
        SourceFormat = sourceFormat;
        ReasonCode = reasonCode;
    }

    /// <summary>pdf | epub | txt</summary>
    public string SourceFormat { get; }

    /// <summary>e.g. PDF_NO_TEXT_LAYER</summary>
    public string ReasonCode { get; }
}

namespace AggregatorService.Dtos;

/// <summary>
/// Ответ после загрузки PDF-документа в хранилище Reader.
/// </summary>
public class UploadDocumentResponseDto
{
    /// <summary>
    /// Публичный или presigned URL загруженного PDF.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// UUID загруженного документа.
    /// </summary>
    public string? DocumentId { get; set; }
}

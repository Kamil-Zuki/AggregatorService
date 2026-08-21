namespace AggregatorService.Dtos;

/// <summary>
/// Ответ после загрузки изображения в хранилище (для редактора карточек).
/// </summary>
public class UploadImageResponseDto
{
    /// <summary>
    /// Публичный или presigned URL загруженного изображения.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// UUID загруженного изображения (для отдачи через serve-image).
    /// </summary>
    public string? ImageId { get; set; }
}

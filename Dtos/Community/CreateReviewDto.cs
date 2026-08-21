namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для создания отзыва (SR-MKT-05)
/// </summary>
public class CreateReviewDto
{
    /// <summary>
    /// ID товара
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Рейтинг (1-5)
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Текст отзыва
    /// </summary>
    public string? Comment { get; set; }
}

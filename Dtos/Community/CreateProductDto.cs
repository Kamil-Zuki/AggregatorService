namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для создания товара (SR-MKT-01)
/// </summary>
public class CreateProductDto
{
    /// <summary>
    /// ID колоды
    /// </summary>
    public Guid DeckId { get; set; }

    /// <summary>
    /// Название товара
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание в HTML
    /// </summary>
    public string? DescriptionHtml { get; set; }

    /// <summary>
    /// URL обложки
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Цена
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Валюта (по умолчанию USD)
    /// </summary>
    public string Currency { get; set; } = "USD";
}

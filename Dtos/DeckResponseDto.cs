namespace AggregatorService.Dtos;

/// <summary>
/// DTO для ответа с данными колоды
/// </summary>
public class DeckResponseDto
{
    /// <summary>
    /// Идентификатор колоды
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор проекта
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор родительской колоды (nullable)
    /// </summary>
    public string? ParentDeckId { get; set; }

    /// <summary>
    /// Идентификатор владельца
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Название колоды
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание колоды
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL обложки колоды
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Флаг публичности колоды
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Политика вклада
    /// </summary>
    public ContributionPolicyDto ContributionPolicy { get; set; }

    /// <summary>
    /// Тип лицензии
    /// </summary>
    public LicenseTypeDto LicenseType { get; set; }

    /// <summary>
    /// Идентификатор колоды, от которой была создана эта (nullable)
    /// </summary>
    public string? ForkedFromId { get; set; }

    /// <summary>
    /// Количество карточек в колоде
    /// </summary>
    public int CardCount { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Enum для типа лицензии
/// </summary>
public enum LicenseTypeDto
{
    Private = 0,
    FreeAttribution = 1,
    Commercial = 2,
    CommercialDerivative = 3
}


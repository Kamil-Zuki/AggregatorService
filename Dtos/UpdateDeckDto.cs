using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos;

/// <summary>
/// DTO для обновления колоды через REST API
/// </summary>
public class UpdateDeckDto
{
    /// <summary>
    /// Название колоды (опционально)
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    public string? Title { get; set; }

    /// <summary>
    /// Описание колоды (опционально)
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Идентификатор родительской колоды (опционально)
    /// </summary>
    public string? ParentDeckId { get; set; }

    /// <summary>
    /// Флаг публичности колоды (опционально)
    /// </summary>
    public bool? IsPublic { get; set; }

    /// <summary>
    /// URL обложки колоды (опционально)
    /// </summary>
    [StringLength(500)]
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Политика вклада (опционально)
    /// </summary>
    public ContributionPolicyDto? ContributionPolicy { get; set; }
}

/// <summary>
/// Enum для политики вклада в колоду
/// </summary>
public enum ContributionPolicyDto
{
    Open = 0,
    Restricted = 1,
    Closed = 2
}


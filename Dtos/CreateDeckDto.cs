using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos;

/// <summary>
/// DTO для создания колоды через REST API
/// </summary>
public class CreateDeckDto
{
    /// <summary>
    /// Идентификатор проекта
    /// </summary>
    [Required]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Название колоды
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

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
    /// Флаг публичности колоды
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// URL обложки колоды (опционально)
    /// </summary>
    [StringLength(500)]
    public string? CoverImageUrl { get; set; }
}


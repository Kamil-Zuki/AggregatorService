namespace AggregatorService.Dtos;

/// <summary>
/// DTO для пагинированного ответа
/// </summary>
public class PaginatedResponseDto<T>
{
    /// <summary>
    /// Элементы на странице
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Номер текущей страницы
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Общее количество страниц
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Общее количество элементов
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Есть ли предыдущая страница
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Есть ли следующая страница
    /// </summary>
    public bool HasNextPage { get; set; }
}

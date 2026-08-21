namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для ответа с предложением
/// </summary>
public class ContributionResponseDto
{
    public Guid Id { get; set; }
    public Guid TargetDeckId { get; set; }
    public Guid? TargetCardId { get; set; }
    public AuthorInfoDto Author { get; set; } = new();
    public string Type { get; set; } = string.Empty; // EDIT, ADD, DELETE
    public string Status { get; set; } = string.Empty; // PENDING, MERGED, REJECTED
    public CardContentDto Content { get; set; } = new();
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Информация об авторе
/// </summary>
public class AuthorInfoDto
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
}

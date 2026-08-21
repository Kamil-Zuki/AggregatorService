namespace AggregatorService.Dtos;

/// <summary>
/// DTO для отправки оценки карточки (SR-LRN-03)
/// </summary>
public class ReviewCardRequestDto
{
    public string CardId { get; set; } = string.Empty;
    public int Rating { get; set; } // 1=Again, 2=Hard, 3=Good, 4=Easy
    public int DurationMs { get; set; }
    public string? UserAnswer { get; set; } // Опциональный текстовый ответ пользователя
}

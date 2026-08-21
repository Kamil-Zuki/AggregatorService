namespace AggregatorService.Dtos;

/// <summary>
/// DTO для ответа после отправки оценки
/// </summary>
public class ReviewResponseDto
{
    public string CardId { get; set; } = string.Empty;
    public DateTime NextReviewDate { get; set; }
    public string Interval { get; set; } = string.Empty; // e.g., "3d", "2w"
    public string State { get; set; } = "NEW"; // NEW, LEARNING, REVIEW, RELEARNING
    public double Stability { get; set; }
    public bool IsLeech { get; set; }
    public int BuriedSiblingsCount { get; set; }
    public AnswerValidationResultDto? AnswerValidation { get; set; } // Результат проверки ответа (если user_answer был предоставлен)
}

/// <summary>
/// Результат проверки ответа пользователя
/// </summary>
public class AnswerValidationResultDto
{
    public bool IsCorrect { get; set; }
    public bool IsFuzzyMatch { get; set; }
    public string? MatchedSynonym { get; set; }
    public double SimilarityScore { get; set; }
    public string? Suggestion { get; set; }
}

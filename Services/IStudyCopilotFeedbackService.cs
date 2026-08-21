using AggregatorService.Dtos;

namespace AggregatorService.Services;

/// <summary>
/// Генерация короткой обратной связи после ревью карты (Study Copilot).
/// </summary>
public interface IStudyCopilotFeedbackService
{
    Task<CopilotReviewFeedbackDto> GetFeedbackAsync(
        CopilotReviewFeedbackRequestDto request,
        CancellationToken cancellationToken = default);
}

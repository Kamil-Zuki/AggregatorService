using AggregatorService.Dtos;
using AggregatorService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorService.Controllers;

/// <summary>
/// REST-обёртка для автоматизации: copilot, эксперименты и т.д.
/// </summary>
[ApiController]
[Route("api/automation")]
[Authorize]
public class AutomationController : ControllerBase
{
    private readonly ILogger<AutomationController> _logger;
    private readonly IStudyCopilotFeedbackService _studyCopilot;
    private readonly IAutomationJobOrchestrator _jobOrchestrator;

    public AutomationController(
        ILogger<AutomationController> logger,
        IStudyCopilotFeedbackService studyCopilot,
        IAutomationJobOrchestrator jobOrchestrator)
    {
        _logger = logger;
        _studyCopilot = studyCopilot;
        _jobOrchestrator = jobOrchestrator;
    }

    /// <summary>
    /// Обратная связь Copilot после оценки карты: внешний LLM через OpenAI-compatible API (см. Ai:* в appsettings).
    /// </summary>
    [HttpPost("copilot/review-feedback")]
    [ProducesResponseType(typeof(CopilotReviewFeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<CopilotReviewFeedbackDto> PostCopilotReviewFeedback(
        [FromBody] CopilotReviewFeedbackRequestDto? body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.CardId))
            return BadRequest(new { error = "Нужен корректный cardId." });

        return Ok(new CopilotReviewFeedbackDto
        {
            Tone = "neutral",
            Explanation = string.Empty,
            ActionHint = string.Empty,
            SuggestRemedialCards = false,
            RemedialCards = []
        });
    }

    /// <summary>
    /// Назначение варианта A/B для UI (study-copilot, autopilot).
    /// Без внешнего пайплайна — стабильный control; иначе фронтенд получает 404 и блокирует сессию.
    /// </summary>
    [HttpGet("experiments/assignment")]
    [ProducesResponseType(typeof(ExperimentAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<ExperimentAssignmentDto> GetExperimentAssignment([FromQuery] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(new { error = "Параметр key обязателен." });

        return Ok(new ExperimentAssignmentDto
        {
            Key = key.Trim(),
            Variant = "control"
        });
    }

    /// <summary>
    /// Трекинг событий эксперимента (пока no-op, лог в Debug).
    /// </summary>
    [HttpPost("experiments/events")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult TrackExperimentEvent([FromBody] TrackExperimentEventDto? body)
    {
        if (body == null
            || string.IsNullOrWhiteSpace(body.Key)
            || string.IsNullOrWhiteSpace(body.Variant)
            || string.IsNullOrWhiteSpace(body.EventName))
        {
            return BadRequest(new { error = "Нужны key, variant и eventName." });
        }

        _logger.LogDebug(
            "Experiment event (no-op): key={Key} variant={Variant} event={Event} project={Project} deck={Deck}",
            body.Key,
            body.Variant,
            body.EventName,
            body.ProjectId,
            body.DeckId);

        return NoContent();
    }

    /// <summary>Создать фоновый джоб автоматизации (card-janitor, deep-miner).</summary>
    [HttpPost("jobs")]
    [ProducesResponseType(typeof(AutomationJobDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AutomationJobDto> CreateJob([FromBody] CreateAutomationJobDto? body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.Type))
            return BadRequest(new { error = "Тело запроса должно содержать type." });

        var allowedTypes = new[] { "card-janitor", "deep-miner", "import" };
        if (!allowedTypes.Contains(body.Type.ToLowerInvariant()))
            return BadRequest(new { error = $"Неподдерживаемый тип джоба. Доступны: {string.Join(", ", allowedTypes)}." });

        var payload = body.Payload ?? new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(body.ProjectId))
            payload["projectId"] = body.ProjectId;
        if (!string.IsNullOrWhiteSpace(body.DeckId))
            payload["deckId"] = body.DeckId;
        if (body.ItemsCount.HasValue)
            payload["itemsCount"] = body.ItemsCount.Value;

        var job = _jobOrchestrator.CreateJob(body.Type.ToLowerInvariant(), payload);
        _jobOrchestrator.EnqueueRun(job.Id);

        _logger.LogInformation("Automation job {JobId} of type {JobType} created by user", job.Id, job.Type);
        return Accepted($"/api/automation/jobs/{job.Id}", job);
    }

    /// <summary>Получить состояние фонового джоба (polling UI).</summary>
    [HttpGet("jobs/{id}")]
    [ProducesResponseType(typeof(AutomationJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AutomationJobDto> GetJob(string id)
    {
        var job = _jobOrchestrator.GetJob(id);
        if (job == null)
            return NotFound(new { error = "Джоб не найден." });

        return Ok(job);
    }
}

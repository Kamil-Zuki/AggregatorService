using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AggregatorService.Services;
using Pvs.Content.Grpc;

namespace AggregatorService.Controllers;

public record TrackSkillRequest(int SkillTypeId, int Value);

[ApiController]
[Route("api/v1/projects/{projectId}/autopilot")]
[Authorize]
[AggregatorService.Filters.FeatureFlagFilter("EnableAIAgents")]
public class AutopilotController : ControllerBase
{
    private readonly IVocabularyServiceClient _vocabularyClient;
    private readonly ILogger<AutopilotController> _logger;

    public AutopilotController(IVocabularyServiceClient vocabularyClient, ILogger<AutopilotController> logger)
    {
        _vocabularyClient = vocabularyClient;
        _logger = logger;
    }

    [HttpGet("daily-plan")]
    public async Task<IActionResult> GetDailyPlan(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = AggregatorService.Helpers.MappingHelper.GetUserId(User, Request.Headers);
            var plan = await _vocabularyClient.GetDailyAutopilotPlanAsync(projectId.ToString(), userId, cancellationToken);
            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily autopilot plan");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Records user skill activity for today (reading minutes, writing/speaking exercises).
    /// Value is accumulated via upsert — calling multiple times adds up.
    /// SkillTypeId: 1=reading, 2=listening, 3=writing, 4=speaking.
    /// </summary>
    [HttpPost("track-skill")]
    public async Task<IActionResult> TrackSkill(
        Guid projectId,
        [FromBody] TrackSkillRequest body,
        CancellationToken cancellationToken)
    {
        if (body.SkillTypeId <= 0 || body.Value <= 0)
            return BadRequest("SkillTypeId and Value must be positive integers.");

        try
        {
            var userId = AggregatorService.Helpers.MappingHelper.GetUserId(User, Request.Headers);
            var result = await _vocabularyClient.TrackSkillActivityAsync(
                projectId.ToString(), userId, body.SkillTypeId, body.Value, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking skill activity");
            return StatusCode(500, "Internal server error");
        }
    }
}

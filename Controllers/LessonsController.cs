using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pvs.Content.Grpc;
using Grpc.Core;
using System.Security.Claims;
using AggregatorService.Options;
using AggregatorService.Services;
using AggregatorService.Helpers;

namespace AggregatorService.Controllers;

/// <summary>Body DTO for POST /complete</summary>
public record CompleteLessonBody(int ScorePercent = 0, int TimeSpentSeconds = 0);


[ApiController]
[Route("api/projects/{projectId:guid}/[controller]")]
[Authorize]
[AggregatorService.Filters.FeatureFlagFilter("EnableAdvancedModules")]
public class LessonsController : ControllerBase
{
    private readonly LessonService.LessonServiceClient _lessonClient;
    private readonly IAgentServiceClient _agentServiceClient;
    private readonly ILogger<LessonsController> _logger;

    public LessonsController(
        LessonService.LessonServiceClient lessonClient,
        IAgentServiceClient agentServiceClient,
        ILogger<LessonsController> logger)
    {
        _lessonClient = lessonClient;
        _agentServiceClient = agentServiceClient;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private Metadata GetHeaders()
    {
        var headers = new Metadata
        {
            { "x-user-id", GetUserId() }
        };

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        foreach (var role in roles)
        {
            headers.Add("x-user-role", role);
        }

        return headers;
    }

    [HttpGet]
    public async Task<IActionResult> GetLessons(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _lessonClient.GetLessonsAsync(new GetLessonsRequest
            {
                UserId = GetUserId()
            }, headers: GetHeaders(), cancellationToken: cancellationToken);

            return Ok(response);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(ex.Status.Detail);
        }
    }

    [HttpGet("{lessonId:guid}")]
    public async Task<IActionResult> GetLesson(Guid projectId, Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _lessonClient.GetLessonAsync(new GetLessonRequest
            {
                UserId = GetUserId(),
                LessonId = lessonId.ToString()
            }, headers: GetHeaders(), cancellationToken: cancellationToken);

            return Ok(response);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(ex.Status.Detail);
        }
    }

    [HttpPost("{lessonId:guid}/start")]
    public async Task<IActionResult> StartLesson(Guid projectId, Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            var userIdStr = GetUserId();
            var headers = GetHeaders();
            
            var getResponse = await _lessonClient.GetLessonAsync(new GetLessonRequest
            {
                UserId = userIdStr,
                LessonId = lessonId.ToString()
            }, headers: headers, cancellationToken: cancellationToken);

            var lesson = getResponse.LessonWithProgress.Lesson;
            var progress = getResponse.LessonWithProgress.Progress;

            string? threadId = progress?.AgentThreadId;

            if (string.IsNullOrEmpty(threadId))
            {
                var userId = MappingHelper.GetUserId(User, Request.Headers);
                var roles = MappingHelper.GetRoles(User, Request.Headers);

                var createThreadReq = new Pvs.Agent.Grpc.CreateAgentThreadRequest 
                { 
                    ProjectId = projectId.ToString()
                };

                if (!string.IsNullOrEmpty(lesson.SystemPrompt))
                {
                    createThreadReq.SystemPromptOverride = lesson.SystemPrompt;
                }

                var threadResponse = await _agentServiceClient.CreateAgentThreadAsync(
                    createThreadReq,
                    userId,
                    roles,
                    cancellationToken);

                threadId = threadResponse.Id;
            }

            var startResponse = await _lessonClient.StartLessonAsync(new StartLessonRequest
            {
                UserId = userIdStr,
                LessonId = lessonId.ToString(),
                AgentThreadId = threadId
            }, headers: headers, cancellationToken: cancellationToken);

            return Ok(startResponse);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(ex.Status.Detail);
        }
    }

    [HttpPost("{lessonId:guid}/restart")]
    public async Task<IActionResult> RestartLesson(Guid projectId, Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            var userIdStr = GetUserId();
            var headers = GetHeaders();
            
            var getResponse = await _lessonClient.GetLessonAsync(new GetLessonRequest
            {
                UserId = userIdStr,
                LessonId = lessonId.ToString()
            }, headers: headers, cancellationToken: cancellationToken);

            var lesson = getResponse.LessonWithProgress.Lesson;
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var createThreadReq = new Pvs.Agent.Grpc.CreateAgentThreadRequest 
            { 
                ProjectId = projectId.ToString()
            };

            if (!string.IsNullOrEmpty(lesson.SystemPrompt))
            {
                createThreadReq.SystemPromptOverride = lesson.SystemPrompt;
            }

            var threadResponse = await _agentServiceClient.CreateAgentThreadAsync(
                createThreadReq,
                userId,
                roles,
                cancellationToken);

            var threadId = threadResponse.Id;

            var startResponse = await _lessonClient.StartLessonAsync(new StartLessonRequest
            {
                UserId = userIdStr,
                LessonId = lessonId.ToString(),
                AgentThreadId = threadId
            }, headers: headers, cancellationToken: cancellationToken);

            return Ok(startResponse);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(ex.Status.Detail);
        }
    }

    [HttpPost("{lessonId:guid}/complete")]
    public async Task<IActionResult> CompleteLesson(Guid projectId, Guid lessonId, [FromBody] CompleteLessonBody? body, CancellationToken cancellationToken)
    {
        try
        {
            await _lessonClient.CompleteLessonAsync(new CompleteLessonRequest
            {
                UserId = GetUserId(),
                LessonId = lessonId.ToString(),
                ScorePercent = body?.ScorePercent ?? 0,
                TimeSpentSeconds = body?.TimeSpentSeconds ?? 0
            }, headers: GetHeaders(), cancellationToken: cancellationToken);

            return Ok();
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(ex.Status.Detail);
        }
    }
}

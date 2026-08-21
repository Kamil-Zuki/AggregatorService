using AggregatorService.Dtos;
using AggregatorService.Helpers;
using AggregatorService.Services;
using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pvs.Content.Grpc;

namespace AggregatorService.Controllers;

/// <summary>
/// Контроллер для работы с аналитикой
/// </summary>
[ApiController]
[Route("api/analytics")]
[Authorize] // Требуем аутентификацию через JWT токен от authorization-module для всех методов
public class AnalyticsController : ControllerBase
{
    private readonly IVocabularyServiceClient _vocabularyServiceClient;
    private readonly ILogger<AnalyticsController> _logger;
    private readonly IMapper _mapper;

    public AnalyticsController(
        IVocabularyServiceClient vocabularyServiceClient,
        ILogger<AnalyticsController> logger,
        IMapper mapper)
    {
        _vocabularyServiceClient = vocabularyServiceClient;
        _logger = logger;
        _mapper = mapper;
    }

    //===== SR-ANL-01: Оценка словарного запаса =====
    /// <summary>
    /// Получает статистику словарного запаса для проекта (SR-ANL-01)
    /// </summary>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <returns>Статистика словарного запаса</returns>
    [HttpGet("vocabulary")]
    [ProducesResponseType(typeof(VocabularyStatsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VocabularyStatsResponseDto>> GetVocabularyStats([FromQuery] string projectId)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            if (string.IsNullOrEmpty(projectId))
            {
                return BadRequest(new { error = "ProjectId is required" });
            }

            _logger.LogInformation(
                "GetVocabularyStats request from user {UserId} for project {ProjectId}",
                userId,
                projectId);

            var grpcRequest = new GetVocabularyStatsRequest
            {
                ProjectId = projectId
            };

            var grpcResponse = await _vocabularyServiceClient.GetVocabularyStatsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<VocabularyStatsResponseDto>(grpcResponse);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting vocabulary stats");
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vocabulary stats");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== Phase 2: Skill Balance =====
    /// <summary>
    /// Получает баланс навыков (Skill Tracking) для проекта
    /// </summary>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <returns>Оценка по 4 навыкам (0-100)</returns>
    [HttpGet("skills")]
    [ProducesResponseType(typeof(SkillBalanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SkillBalanceResponseDto>> GetSkillBalance([FromQuery] string projectId)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            if (string.IsNullOrEmpty(projectId))
            {
                return BadRequest(new { error = "ProjectId is required" });
            }

            _logger.LogInformation(
                "GetSkillBalance request from user {UserId} for project {ProjectId}",
                userId,
                projectId);

            var grpcRequest = new GetSkillBalanceRequest
            {
                ProjectId = projectId
            };

            var grpcResponse = await _vocabularyServiceClient.GetSkillBalanceAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<SkillBalanceResponseDto>(grpcResponse);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting skill balance");
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skill balance");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-ANL-02: Календарь активности =====
    /// <summary>
    /// Получает данные для календаря активности (heatmap) (SR-ANL-02)
    /// </summary>
    /// <param name="projectId">Идентификатор проекта (опционально)</param>
    /// <param name="year">Год для отображения (по умолчанию текущий)</param>
    /// <returns>Данные для календаря активности</returns>
    [HttpGet("heatmap")]
    [ProducesResponseType(typeof(HeatmapResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HeatmapResponseDto>> GetHeatmap(
        [FromQuery] string? projectId = null,
        [FromQuery] int? year = null)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            var requestYear = year ?? DateTime.UtcNow.Year;

            _logger.LogInformation(
                "GetHeatmap request from user {UserId} for year {Year}, projectId: {ProjectId}",
                userId,
                requestYear,
                projectId ?? "all");

            var grpcRequest = new GetHeatmapRequest
            {
                Year = requestYear
            };

            if (!string.IsNullOrEmpty(projectId))
            {
                grpcRequest.ProjectId = projectId;
            }

            var grpcResponse = await _vocabularyServiceClient.GetHeatmapAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<HeatmapResponseDto>(grpcResponse);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting heatmap");
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting heatmap");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-ANL-03: Дневная сводка =====
    /// <summary>
    /// Получает дневную сводку и информацию о серии (SR-ANL-03)
    /// </summary>
    /// <param name="timezoneOffset">Смещение часового пояса в минутах от UTC (опционально)</param>
    /// <returns>Дневная сводка</returns>
    [HttpGet("daily")]
    [ProducesResponseType(typeof(DailySummaryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DailySummaryResponseDto>> GetDailySummary([FromQuery] int? timezoneOffset = null)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetDailySummary request from user {UserId}, timezoneOffset: {TimezoneOffset}",
                userId,
                timezoneOffset?.ToString() ?? "not specified");

            var grpcRequest = new GetDailySummaryRequest();

            if (timezoneOffset.HasValue)
            {
                grpcRequest.TimezoneOffset = timezoneOffset.Value;
            }

            var grpcResponse = await _vocabularyServiceClient.GetDailySummaryAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<DailySummaryResponseDto>(grpcResponse);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting daily summary");
            // When VocabularyService is unavailable (e.g. not running), return defaults so the app stays functional
            var isUnavailable = ex.StatusCode is Grpc.Core.StatusCode.Unavailable
                or Grpc.Core.StatusCode.DeadlineExceeded
                or Grpc.Core.StatusCode.Unimplemented;
            if (isUnavailable)
            {
                _logger.LogWarning("VocabularyService unavailable, returning default daily summary");
                var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                return Ok(new DailySummaryResponseDto
                {
                    Date = today,
                    CurrentStreak = 0,
                    IsStreakExtendedToday = false,
                    TimeSpentSeconds = 0,
                    NewCards = new GoalProgressDto { Current = 0, Target = 20, IsCompleted = false },
                    Reviews = new GoalProgressDto { Current = 0, Target = 100, IsCompleted = false }
                });
            }

            var detail = ex.Status.Detail ?? "Vocabulary service error";
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Error getting daily summary",
                Status = statusCode,
                Detail = detail
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily summary");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Failed to get daily summary. Please try again."
            });
        }
    }
}

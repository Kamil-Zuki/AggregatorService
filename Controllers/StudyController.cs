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
/// Контроллер для работы с обучением (Study Service)
/// </summary>
[ApiController]
[Route("api/study")]
[Authorize] // Требуем аутентификацию через JWT токен от authorization-module для всех методов
public class StudyController : ControllerBase
{
    private readonly IVocabularyServiceClient _vocabularyServiceClient;
    private readonly ILogger<StudyController> _logger;
    private readonly IMapper _mapper;

    public StudyController(
        IVocabularyServiceClient vocabularyServiceClient,
        ILogger<StudyController> logger,
        IMapper mapper)
    {
        _vocabularyServiceClient = vocabularyServiceClient;
        _logger = logger;
        _mapper = mapper;
    }

    //===== SR-LRN-01: Старт новой сессии обучения =====
    /// <summary>
    /// Старт новой сессии обучения (SR-LRN-01)
    /// </summary>
    /// <param name="request">Данные для старта сессии</param>
    /// <returns>Информация о созданной сессии</returns>
    [HttpPost("session")]
    [ProducesResponseType(typeof(StudySessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudySessionDto>> StartSession([FromBody] StartSessionRequestDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "StartSession request from user {UserId} for project {ProjectId}",
                userId,
                request.ProjectId);

            var grpcRequest = _mapper.Map<StartStudySessionRequest>(request);

            var grpcResponse = await _vocabularyServiceClient.StartStudySessionAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<StudySessionDto>(grpcResponse);

            _logger.LogInformation(
                "Session {SessionId} started successfully for user {UserId}",
                responseDto.Id,
                userId);

            return CreatedAtAction(
                nameof(GetNextCard),
                new { id = responseDto.Id },
                responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to start session");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when starting session");
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
            _logger.LogError(ex, "Error starting session");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-LRN-02: Получение следующей карточки =====
    /// <summary>
    /// Получение следующей карточки из очереди сессии (SR-LRN-02)
    /// </summary>
    /// <param name="id">ID активной сессии</param>
    /// <returns>Следующая карточка для обучения</returns>
    [HttpGet("session/{id}/next")]
    [ProducesResponseType(typeof(Dtos.CardStudyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)] // Сессия завершена
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Dtos.CardStudyDto>> GetNextCard(string id)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetNextCard request from user {UserId} for session {SessionId}",
                userId,
                id);

            var grpcRequest = new GetNextCardRequest
            {
                UserId = userId.ToString(),
                SessionId = id
            };

            var grpcResponse = await _vocabularyServiceClient.GetNextCardAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            // Если карточка не найдена (сессия завершена), возвращаем 204
            if (grpcResponse.Card == null)
            {
                return NoContent();
            }

            var responseDto = _mapper.Map<Dtos.CardStudyDto>(grpcResponse.Card);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to get next card");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting next card");
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };
            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next card");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-LRN-03: Отправка оценки (FSRS) =====
    /// <summary>
    /// Отправка оценки карточки (SR-LRN-03)
    /// </summary>
    /// <param name="id">ID активной сессии</param>
    /// <param name="request">Данные оценки</param>
    /// <returns>Результат обработки оценки</returns>
    [HttpPost("session/{id}/review")]
    [ProducesResponseType(typeof(ReviewResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReviewResponseDto>> SubmitReview(string id, [FromBody] ReviewCardRequestDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "SubmitReview request from user {UserId} for session {SessionId}, card {CardId}, rating {Rating}",
                userId,
                id,
                request.CardId,
                request.Rating);

            var grpcRequest = _mapper.Map<SubmitReviewRequest>(request);
            grpcRequest.SessionId = id;

            var grpcResponse = await _vocabularyServiceClient.SubmitReviewAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<ReviewResponseDto>(grpcResponse);

            _logger.LogInformation(
                "Review submitted successfully for card {CardId}, next review: {NextReviewDate}",
                responseDto.CardId,
                responseDto.NextReviewDate);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to submit review");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when submitting review");
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
            _logger.LogError(ex, "Error submitting review");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-LRN-08: Отмена последнего действия =====
    /// <summary>
    /// Отмена последнего действия в сессии (SR-LRN-08)
    /// </summary>
    /// <param name="id">ID активной сессии</param>
    /// <param name="request">Запрос на отмену (может быть пустым)</param>
    /// <returns>Результат отмены действия</returns>
    [HttpPost("session/{id}/undo")]
    [ProducesResponseType(typeof(UndoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UndoResponseDto>> UndoReview(string id, [FromBody] UndoReviewRequestDto? request = null)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "UndoReview request from user {UserId} for session {SessionId}",
                userId,
                id);

            var grpcRequest = new UndoReviewRequest
            {
                UserId = userId.ToString(),
                SessionId = id
            };

            var grpcResponse = await _vocabularyServiceClient.UndoReviewAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<UndoResponseDto>(grpcResponse);

            _logger.LogInformation(
                "Review undone successfully, restored card {CardId}",
                responseDto.RestoredCardId);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to undo review");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when undoing review");
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
            _logger.LogError(ex, "Error undoing review");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }
}

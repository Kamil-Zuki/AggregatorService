using AggregatorService.Dtos;
using AggregatorService.Helpers;
using AggregatorService.Services;
using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorService.Controllers;

/// <summary>
/// Контроллер для работы с настройками пользователя
/// </summary>
[ApiController]
[Route("api/settings")]
[Authorize] // Требуем аутентификацию через JWT токен от authorization-module для всех методов
public class UserSettingsController : ControllerBase
{
    private readonly IVocabularyServiceClient _vocabularyServiceClient;
    private readonly ILogger<UserSettingsController> _logger;
    private readonly IMapper _mapper;

    public UserSettingsController(
        IVocabularyServiceClient vocabularyServiceClient,
        ILogger<UserSettingsController> logger,
        IMapper mapper)
    {
        _vocabularyServiceClient = vocabularyServiceClient;
        _logger = logger;
        _mapper = mapper;
    }

    //===== SR-SETT-01: Получение настроек пользователя =====
    /// <summary>
    /// Получает глобальные настройки пользователя (SR-SETT-01)
    /// </summary>
    /// <returns>Настройки пользователя</returns>
    [HttpGet]
    [ProducesResponseType(typeof(UserSettingsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSettingsResponseDto>> GetUserSettings()
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "GetUserSettings request from user {UserId}",
                userId);

            var grpcRequest = new Pvs.Content.Grpc.GetUserSettingsRequest();

            var grpcResponse = await _vocabularyServiceClient.GetUserSettingsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<UserSettingsResponseDto>(grpcResponse);

            _logger.LogInformation(
                "User settings retrieved successfully for user {UserId}",
                userId);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting user settings");
            
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
            _logger.LogError(ex, "Error getting user settings");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    //===== SR-SETT-01: Обновление настроек пользователя =====
    /// <summary>
    /// Обновляет глобальные настройки пользователя (SR-SETT-01)
    /// </summary>
    /// <param name="request">Данные для обновления настроек</param>
    /// <returns>Обновленные настройки пользователя</returns>
    [HttpPut]
    [ProducesResponseType(typeof(UserSettingsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSettingsResponseDto>> UpdateUserSettings([FromBody] UpdateUserSettingsDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            var roles = MappingHelper.GetRoles(User, Request.Headers);

            _logger.LogInformation(
                "UpdateUserSettings request from user {UserId}",
                userId);

            var grpcRequest = _mapper.Map<Pvs.Content.Grpc.UpdateUserSettingsRequest>(request);

            var grpcResponse = await _vocabularyServiceClient.UpdateUserSettingsAsync(
                grpcRequest,
                userId,
                roles,
                HttpContext.RequestAborted);

            var responseDto = _mapper.Map<UserSettingsResponseDto>(grpcResponse);

            _logger.LogInformation(
                "User settings updated successfully for user {UserId}",
                userId);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating user settings");
            
            var statusCode = ex.StatusCode switch
            {
                Grpc.Core.StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                Grpc.Core.StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                Grpc.Core.StatusCode.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user settings");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }
}


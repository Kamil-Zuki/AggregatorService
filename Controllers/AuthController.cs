using AggregatorService.Dtos.Auth;
using AggregatorService.Helpers;
using AggregatorService.Services;
using Grpc.Core;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using GrpcStatusCode = Grpc.Core.StatusCode;

namespace AggregatorService.Controllers;

/// <summary>
/// Контроллер для проксирования запросов авторизации к authorization-module
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthorizationServiceClient _authorizationClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthorizationServiceClient authorizationClient,
        ILogger<AuthController> logger)
    {
        _authorizationClient = authorizationClient;
        _logger = logger;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth-public")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] UserRegistrationDto request)
    {
        try
        {
            _logger.LogInformation("Register request for email: {Email}", request.Email);

            var result = await _authorizationClient.RegisterAsync(request, HttpContext.RequestAborted);

            return Created(string.Empty, result);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when registering user: {Email}", request.Email);
            
            // Преобразуем gRPC статус коды в HTTP статус коды
            var httpStatusCode = ex.StatusCode switch
            {
                GrpcStatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                GrpcStatusCode.AlreadyExists => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(httpStatusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when registering user: {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Вход пользователя
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth-public")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] UserLoginDto request)
    {
        try
        {
            _logger.LogInformation("Login request for email: {Email}", request.Email);

            var result = await _authorizationClient.LoginAsync(request, HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when logging in user: {Email}", request.Email);
            
            // Преобразуем gRPC статус коды в HTTP статус коды
            var httpStatusCode = ex.StatusCode switch
            {
                GrpcStatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
                GrpcStatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(httpStatusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when logging in user: {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Обновление токена
    /// </summary>
    [HttpPost("refresh-token")]
    [EnableRateLimiting("auth-public")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken([FromBody] RefreshTokenDto request)
    {
        try
        {
            _logger.LogInformation("Refresh token request");

            var result = await _authorizationClient.RefreshTokenAsync(request, HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when refreshing token");
            
            // Преобразуем gRPC статус коды в HTTP статус коды
            var httpStatusCode = ex.StatusCode switch
            {
                GrpcStatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
                GrpcStatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(httpStatusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when refreshing token");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Подтверждение email
    /// </summary>
    [HttpGet("confirm-email")]
    [EnableRateLimiting("auth-public")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponseDto>> ConfirmEmail([FromQuery] ConfirmEmailDto request)
    {
        try
        {
            _logger.LogInformation("Confirm email request for userId: {UserId}", request.UserId);

            var result = await _authorizationClient.ConfirmEmailAsync(request, HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when confirming email for userId: {UserId}", request.UserId);
            
            // Преобразуем gRPC статус коды в HTTP статус коды
            var httpStatusCode = ex.StatusCode switch
            {
                GrpcStatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                GrpcStatusCode.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(httpStatusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when confirming email for userId: {UserId}", request.UserId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Повторная отправка письма подтверждения email
    /// </summary>
    [HttpPost("resend-confirmation")]
    [EnableRateLimiting("auth-public")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponseDto>> ResendConfirmation([FromBody] ResendConfirmationEmailDto request)
    {
        try
        {
            _logger.LogInformation("Resend confirmation email request for email: {Email}", request.Email);

            var result = await _authorizationClient.ResendConfirmationEmailAsync(request, HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when resending confirmation email for email: {Email}", request.Email);
            
            var httpStatusCode = ex.StatusCode switch
            {
                GrpcStatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                GrpcStatusCode.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(httpStatusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when resending confirmation email for email: {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Получение информации о текущем пользователе
    /// </summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserInfoDto>> GetUserInfo()
    {
        try
        {
            // Извлекаем user_id из JWT токена
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            
            _logger.LogInformation("Get user info request for userId: {UserId}", userId);

            var result = await _authorizationClient.GetUserInfoAsync(userId, HttpContext.RequestAborted);
            
            result.Roles = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .ToList();

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting user info");
            
            // Преобразуем gRPC статус коды в HTTP статус коды
            var httpStatusCode = ex.StatusCode switch
            {
                GrpcStatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
                GrpcStatusCode.NotFound => StatusCodes.Status404NotFound,
                GrpcStatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(httpStatusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting user info");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Выход пользователя
    /// </summary>
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponseDto>> Logout([FromBody] LogoutDto request)
    {
        try
        {
            // Извлекаем user_id из JWT токена
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            
            _logger.LogInformation("Logout request for userId: {UserId}", userId);

            var result = await _authorizationClient.LogoutAsync(userId, request, HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when logging out");
            
            // Преобразуем gRPC статус коды в HTTP статус коды
            var httpStatusCode = ex.StatusCode switch
            {
                GrpcStatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
                GrpcStatusCode.NotFound => StatusCodes.Status404NotFound,
                GrpcStatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(httpStatusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when logging out");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Обновление имени пользователя
    /// </summary>
    [HttpPut("username")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponseDto>> UpdateUsername([FromBody] UpdateUsernameDto request)
    {
        try
        {
            // Извлекаем user_id из JWT токена
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            
            _logger.LogInformation("Update username request for userId: {UserId}", userId);

            var result = await _authorizationClient.UpdateUsernameAsync(userId, request, HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating username");
            
            // Преобразуем gRPC статус коды в HTTP статус коды
            var httpStatusCode = ex.StatusCode switch
            {
                GrpcStatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
                GrpcStatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                GrpcStatusCode.NotFound => StatusCodes.Status404NotFound,
                GrpcStatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(httpStatusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating username");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Обновление пароля пользователя
    /// </summary>
    [HttpPut("password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponseDto>> UpdatePassword([FromBody] UpdatePasswordDto request)
    {
        try
        {
            // Извлекаем user_id из JWT токена
            var userId = MappingHelper.GetUserId(User, Request.Headers);
            
            _logger.LogInformation("Update password request for userId: {UserId}", userId);

            var result = await _authorizationClient.UpdatePasswordAsync(userId, request, HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating password");
            
            // Преобразуем gRPC статус коды в HTTP статус коды
            var httpStatusCode = ex.StatusCode switch
            {
                GrpcStatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
                GrpcStatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                GrpcStatusCode.NotFound => StatusCodes.Status404NotFound,
                GrpcStatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(httpStatusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating password");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Обновление URL аватара профиля (пустая строка — удалить фото)
    /// </summary>
    [HttpPut("avatar-url")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuthResponseDto>> UpdateAvatarUrl([FromBody] UpdateAvatarUrlDto request)
    {
        try
        {
            var userId = MappingHelper.GetUserId(User, Request.Headers);

            _logger.LogInformation("Update avatar URL request for userId: {UserId}", userId);

            var result = await _authorizationClient.UpdateAvatarUrlAsync(userId, request, HttpContext.RequestAborted);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new { error = ex.Message });
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating avatar URL");

            var httpStatusCode = ex.StatusCode switch
            {
                GrpcStatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
                GrpcStatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
                GrpcStatusCode.NotFound => StatusCodes.Status404NotFound,
                GrpcStatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(httpStatusCode, new { error = ex.Status.Detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating avatar URL");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error occurred" });
        }
    }
}


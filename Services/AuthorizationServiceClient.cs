using AggregatorService.Dtos.Auth;
using AggregatorService.Dtos.Admin;
using AggregatorService.Options;
using AutoMapper;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Pvs.Auth.Grpc;
using static Pvs.Auth.Grpc.AuthService;

namespace AggregatorService.Services;

/// <summary>
/// gRPC клиент для работы с authorization-module
/// </summary>
public class AuthorizationServiceClient : IAuthorizationServiceClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly AuthServiceClient _client;
    private readonly AggregatorServiceOptions _options;
    private readonly ILogger<AuthorizationServiceClient> _logger;
    private readonly IMapper _mapper;

    public AuthorizationServiceClient(
        IOptions<AggregatorServiceOptions> options,
        ILogger<AuthorizationServiceClient> logger,
        AuthServiceClient client,
        IMapper mapper)
    {
        _options = options.Value;
        _logger = logger;
        _client = client;
        _mapper = mapper;
    }

    /// <summary>
    /// Регистрация нового пользователя 
    /// </summary>
    public async Task<AuthResponseDto> RegisterAsync(UserRegistrationDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending registration request to authorization-module for email: {Email}", request.Email);

            // Преобразуем DTO в gRPC запрос
            var grpcRequest = _mapper.Map<RegisterUserRequest>(request);

            // Выполняем gRPC вызов
            var response = await _client.RegisterUserAsync(
                grpcRequest,
                cancellationToken: cancellationToken);

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            // Преобразуем gRPC ответ в DTO
            return _mapper.Map<AuthResponseDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when registering user: {Email}", request.Email);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when registering user: {Email}", request.Email);
            throw;
        }
    }

    /// <summary>
    /// Вход пользователя
    /// </summary>
    public async Task<TokenResponseDto> LoginAsync(UserLoginDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending login request to authorization-module for email: {Email}", request.Email);

            // Преобразуем DTO в gRPC запрос
            var grpcRequest = _mapper.Map<LoginUserRequest>(request);

            // Выполняем gRPC вызов
            var response = await _client.LoginUserAsync(
                grpcRequest,
                cancellationToken: cancellationToken);

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            // Преобразуем gRPC ответ в DTO
            return _mapper.Map<TokenResponseDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when logging in user: {Email}", request.Email);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when logging in user: {Email}", request.Email);
            throw;
        }
    }

    /// <summary>
    /// Обновление токена
    /// </summary>
    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending refresh token request to authorization-module");

            // Преобразуем DTO в gRPC запрос
            var grpcRequest = _mapper.Map<RefreshTokenRequest>(request);

            // Выполняем gRPC вызов
            var response = await _client.RefreshTokenAsync(
                grpcRequest,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Token refreshed successfully");

            // Преобразуем gRPC ответ в DTO
            return _mapper.Map<TokenResponseDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when refreshing token");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when refreshing token");
            throw;
        }
    }

    /// <summary>
    /// Подтверждение email
    /// </summary>
    public async Task<AuthResponseDto> ConfirmEmailAsync(ConfirmEmailDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending confirm email request to authorization-module for userId: {UserId}", request.UserId);

            // Преобразуем DTO в gRPC запрос
            var grpcRequest = _mapper.Map<ConfirmEmailRequest>(request);

            // Выполняем gRPC вызов
            var response = await _client.ConfirmEmailAsync(
                grpcRequest,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Email confirmed successfully for userId: {UserId}", request.UserId);

            // Преобразуем gRPC ответ в DTO
            return _mapper.Map<AuthResponseDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when confirming email for userId: {UserId}", request.UserId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when confirming email for userId: {UserId}", request.UserId);
            throw;
        }
    }

    /// <summary>
    /// Повторная отправка письма подтверждения email
    /// </summary>
    public async Task<AuthResponseDto> ResendConfirmationEmailAsync(ResendConfirmationEmailDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending resend confirmation email request to authorization-module for email: {Email}", request.Email);

            var grpcRequest = new ResendConfirmationEmailRequest { Email = request.Email };

            var response = await _client.ResendConfirmationEmailAsync(
                grpcRequest,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Confirmation email resent successfully for email: {Email}", request.Email);

            return _mapper.Map<AuthResponseDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when resending confirmation email for email: {Email}", request.Email);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when resending confirmation email for email: {Email}", request.Email);
            throw;
        }
    }

    /// <summary>
    /// Получение информации о текущем пользователе
    /// </summary>
    public async Task<UserInfoDto> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending get user info request to authorization-module for userId: {UserId}", userId);

            // Создаем метаданные для передачи user_id
            var metadata = new Grpc.Core.Metadata
            {
                { "user_id", userId.ToString() }
            };

            // Преобразуем DTO в gRPC запрос
            var grpcRequest = new GetUserInfoRequest
            {
                UserId = userId.ToString()
            };

            // Выполняем gRPC вызов с метаданными
            var response = await _client.GetUserInfoAsync(
                grpcRequest,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation("User info retrieved successfully for userId: {UserId}", userId);

            // Преобразуем gRPC ответ в DTO
            return _mapper.Map<UserInfoDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting user info for userId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting user info for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserInfoDto> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending find user by email request to authorization-module for email: {Email}", email);

            var response = await _client.FindUserByEmailAsync(
                new FindUserByEmailRequest { Email = email.Trim() },
                cancellationToken: cancellationToken);

            return _mapper.Map<UserInfoDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when finding user by email: {Email}", email);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when finding user by email: {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Выход пользователя
    /// </summary>
    public async Task<AuthResponseDto> LogoutAsync(Guid userId, LogoutDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending logout request to authorization-module for userId: {UserId}", userId);

            // Создаем метаданные для передачи user_id
            var metadata = new Grpc.Core.Metadata
            {
                { "user_id", userId.ToString() }
            };

            // Преобразуем DTO в gRPC запрос
            var grpcRequest = _mapper.Map<LogoutUserRequest>(request);
            grpcRequest.UserId = userId.ToString();

            // Выполняем gRPC вызов с метаданными
            var response = await _client.LogoutUserAsync(
                grpcRequest,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation("User logged out successfully for userId: {UserId}", userId);

            // Преобразуем gRPC ответ в DTO
            return _mapper.Map<AuthResponseDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when logging out user: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when logging out user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Обновление имени пользователя
    /// </summary>
    public async Task<AuthResponseDto> UpdateUsernameAsync(Guid userId, UpdateUsernameDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending update username request to authorization-module for userId: {UserId}", userId);

            // Создаем метаданные для передачи user_id
            var metadata = new Grpc.Core.Metadata
            {
                { "user_id", userId.ToString() }
            };

            // Преобразуем DTO в gRPC запрос
            var grpcRequest = _mapper.Map<UpdateUsernameRequest>(request);
            grpcRequest.UserId = userId.ToString();

            // Выполняем gRPC вызов с метаданными
            var response = await _client.UpdateUsernameAsync(
                grpcRequest,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Username updated successfully for userId: {UserId}", userId);

            // Преобразуем gRPC ответ в DTO
            return _mapper.Map<AuthResponseDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating username for userId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating username for userId: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Обновление пароля пользователя
    /// </summary>
    public async Task<AuthResponseDto> UpdatePasswordAsync(Guid userId, UpdatePasswordDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending update password request to authorization-module for userId: {UserId}", userId);

            // Создаем метаданные для передачи user_id
            var metadata = new Grpc.Core.Metadata
            {
                { "user_id", userId.ToString() }
            };

            // Преобразуем DTO в gRPC запрос
            var grpcRequest = _mapper.Map<UpdatePasswordRequest>(request);
            grpcRequest.UserId = userId.ToString();

            // Выполняем gRPC вызов с метаданными
            var response = await _client.UpdatePasswordAsync(
                grpcRequest,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Password updated successfully for userId: {UserId}", userId);

            // Преобразуем gRPC ответ в DTO
            return _mapper.Map<AuthResponseDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating password for userId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating password for userId: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Обновление URL аватара профиля
    /// </summary>
    public async Task<AuthResponseDto> UpdateAvatarUrlAsync(Guid userId, UpdateAvatarUrlDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending update avatar URL request to authorization-module for userId: {UserId}", userId);

            var metadata = new Grpc.Core.Metadata
            {
                { "user_id", userId.ToString() }
            };

            var grpcRequest = new UpdateAvatarUrlRequest
            {
                UserId = userId.ToString(),
                AvatarUrl = request.AvatarUrl ?? string.Empty
            };

            var response = await _client.UpdateAvatarUrlAsync(
                grpcRequest,
                headers: metadata,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Avatar URL updated successfully for userId: {UserId}", userId);

            return _mapper.Map<AuthResponseDto>(response);
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when updating avatar URL for userId: {UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when updating avatar URL for userId: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Получение списка всех пользователей (для админ-панели)
    /// </summary>
    public async Task<AdminUsersResponseDto> GetUsersListAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending GetUsersList request to authorization-module");

            var grpcRequest = new GetUsersListRequest
            {
                Page = page,
                PageSize = pageSize,
                Search = search ?? string.Empty
            };

            var response = await _client.GetUsersListAsync(
                grpcRequest,
                cancellationToken: cancellationToken);

            var result = new AdminUsersResponseDto
            {
                TotalCount = response.TotalCount
            };

            foreach (var user in response.Users)
            {
                result.Users.Add(new AdminUserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    RegistrationDate = user.RegistrationDate,
                    IsLockedOut = user.IsLockedOut
                });
            }

            return result;
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when getting users list");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when getting users list");
            throw;
        }
    }

    public async Task<AuthResponseDto> AdminSetUserLockoutAsync(string userId, bool lockout, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending AdminSetUserLockout request to authorization-module for {UserId}", userId);

            var response = await _client.AdminSetUserLockoutAsync(
                new AdminSetUserLockoutRequest
                {
                    UserId = userId,
                    Lock = lockout
                },
                cancellationToken: cancellationToken);

            return new AuthResponseDto { Message = response.Message };
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error when setting user lockout");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when setting user lockout");
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}


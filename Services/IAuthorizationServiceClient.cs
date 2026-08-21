using AggregatorService.Dtos.Auth;
using AggregatorService.Dtos.Admin;

namespace AggregatorService.Services;

/// <summary>
/// Интерфейс клиента для работы с authorization-module через gRPC
/// </summary>
public interface IAuthorizationServiceClient
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    Task<AuthResponseDto> RegisterAsync(UserRegistrationDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Вход пользователя
    /// </summary>
    Task<TokenResponseDto> LoginAsync(UserLoginDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновление токена
    /// </summary>
    Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Подтверждение email
    /// </summary>
    Task<AuthResponseDto> ConfirmEmailAsync(ConfirmEmailDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Повторная отправка письма подтверждения email
    /// </summary>
    Task<AuthResponseDto> ResendConfirmationEmailAsync(ResendConfirmationEmailDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение информации о текущем пользователе
    /// </summary>
    Task<UserInfoDto> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Находит пользователя по email для сценариев шаринга.
    /// </summary>
    Task<UserInfoDto> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выход пользователя
    /// </summary>
    Task<AuthResponseDto> LogoutAsync(Guid userId, LogoutDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновление имени пользователя
    /// </summary>
    Task<AuthResponseDto> UpdateUsernameAsync(Guid userId, UpdateUsernameDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновление пароля пользователя
    /// </summary>
    Task<AuthResponseDto> UpdatePasswordAsync(Guid userId, UpdatePasswordDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновление URL аватара профиля
    /// </summary>
    Task<AuthResponseDto> UpdateAvatarUrlAsync(Guid userId, UpdateAvatarUrlDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение списка всех пользователей (для админ-панели)
    /// </summary>
    Task<AdminUsersResponseDto> GetUsersListAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Блокировка/разблокировка пользователя (для админ-панели)
    /// </summary>
    Task<AuthResponseDto> AdminSetUserLockoutAsync(string userId, bool lockout, CancellationToken cancellationToken = default);
}

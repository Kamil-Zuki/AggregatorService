namespace AggregatorService.Dtos.Auth;

/// <summary>
/// DTO для информации о пользователе
/// </summary>
public class UserInfoDto
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Email пользователя
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Флаг подтверждения email
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// URL изображения профиля (https), если задан
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Роли пользователя
    /// </summary>
    public List<string> Roles { get; set; } = new();
}


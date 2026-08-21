using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos.Auth;

/// <summary>
/// DTO для обновления пароля пользователя
/// </summary>
public class UpdatePasswordDto
{
    /// <summary>
    /// Текущий пароль
    /// </summary>
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Новый пароль
    /// </summary>
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}


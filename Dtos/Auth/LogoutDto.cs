using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos.Auth;

/// <summary>
/// DTO для выхода пользователя
/// </summary>
public class LogoutDto
{
    /// <summary>
    /// Токен обновления для инвалидации
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}


using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos.Auth;

/// <summary>
/// DTO для подтверждения email
/// </summary>
public class ConfirmEmailDto
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Токен подтверждения
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;
}


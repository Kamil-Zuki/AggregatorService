using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos.Auth;

/// <summary>
/// DTO для обновления токена
/// </summary>
public class RefreshTokenDto
{
    /// <summary>
    /// Токен обновления
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}


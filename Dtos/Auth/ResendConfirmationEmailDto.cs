using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos.Auth;

/// <summary>
/// DTO для повторной отправки письма подтверждения email
/// </summary>
public class ResendConfirmationEmailDto
{
    /// <summary>
    /// Email пользователя
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Dtos.Auth;

/// <summary>
/// DTO для обновления имени пользователя
/// </summary>
public class UpdateUsernameDto
{
    /// <summary>
    /// Новое имя пользователя
    /// </summary>
    [Required]
    [MinLength(3)]
    public string UserName { get; set; } = string.Empty;
}


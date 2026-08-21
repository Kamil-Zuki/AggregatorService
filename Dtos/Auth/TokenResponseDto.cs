namespace AggregatorService.Dtos.Auth;

/// <summary>
/// DTO для ответа с токенами
/// </summary>
public class TokenResponseDto
{
    /// <summary>
    /// JWT токен доступа
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Токен обновления
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}


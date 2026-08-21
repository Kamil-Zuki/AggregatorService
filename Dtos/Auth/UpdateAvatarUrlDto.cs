namespace AggregatorService.Dtos.Auth;

/// <summary>
/// DTO для обновления URL аватара профиля (пустая строка — сбросить).
/// </summary>
public class UpdateAvatarUrlDto
{
    public string? AvatarUrl { get; set; }
}

using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AggregatorService.Helpers;

/// <summary>
/// Вспомогательный класс для извлечения данных из контекста HTTP запросов
/// </summary>
public static class MappingHelper
{
    /// <summary>
    /// Извлекает user_id из ClaimsPrincipal (JWT токена) или HTTP заголовков
    /// </summary>
    /// <param name="user">ClaimsPrincipal из JWT токена</param>
    /// <param name="headers">HTTP заголовки (для fallback)</param>
    /// <returns>Идентификатор пользователя</returns>
    /// <exception cref="UnauthorizedAccessException">Если user_id не найден</exception>
    public static Guid GetUserId(ClaimsPrincipal? user, IHeaderDictionary? headers)
    {
        // Пытаемся получить из JWT Claims (приоритет - JwtRegisteredClaimNames.Sub, затем ClaimTypes.NameIdentifier)
        // authorization-module использует JwtRegisteredClaimNames.Sub для userId
        var userIdClaim = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("user_id")?.Value;

        if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userIdFromClaim))
        {
            return userIdFromClaim;
        }

        // Fallback: пытаемся получить из HTTP заголовков (для тестирования или специальных случаев)
        if (headers != null && headers.TryGetValue("X-User-Id", out var userIdHeader))
        {
            if (Guid.TryParse(userIdHeader.ToString(), out var userIdFromHeader))
            {
                return userIdFromHeader;
            }
        }

        throw new UnauthorizedAccessException(
            "User ID not found in JWT token. Ensure you are authenticated with a valid JWT token from authorization-module.");
    }

    /// <summary>
    /// Извлекает роли из ClaimsPrincipal (JWT токена) или HTTP заголовков
    /// </summary>
    /// <param name="user">ClaimsPrincipal из JWT токена</param>
    /// <param name="headers">HTTP заголовки (для fallback)</param>
    /// <returns>Список ролей пользователя</returns>
    public static List<string> GetRoles(ClaimsPrincipal? user, IHeaderDictionary? headers)
    {
        var roles = new List<string>();

        // Пытаемся получить из JWT Claims (authorization-module может добавить роли в токен в будущем)
        var roleClaims = user?.FindAll(c => c.Type == ClaimTypes.Role || c.Type == "role");
        if (roleClaims != null)
        {
            roles.AddRange(roleClaims.Select(c => c.Value));
        }

        // Fallback: пытаемся получить из HTTP заголовков (для тестирования или специальных случаев)
        if (headers != null && headers.TryGetValue("X-User-Roles", out var rolesHeader))
        {
            roles.AddRange(rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        return roles.Distinct().ToList();
    }
}

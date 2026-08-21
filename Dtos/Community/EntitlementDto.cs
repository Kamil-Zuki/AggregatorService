namespace AggregatorService.Dtos.Community;

/// <summary>
/// DTO для прав доступа (SR-MKT-03, SR-COL-07)
/// </summary>
public class EntitlementDto
{
    /// <summary>
    /// Есть ли доступ
    /// </summary>
    public bool HasAccess { get; set; }

    /// <summary>
    /// Тип доступа: OWNER, PURCHASED, CONTRIBUTOR, SUBSCRIBER
    /// </summary>
    public string AccessType { get; set; } = string.Empty;

    /// <summary>
    /// Дата истечения (null для вечного доступа)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

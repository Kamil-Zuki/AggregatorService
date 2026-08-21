namespace AggregatorService.Options;

/// <summary>
/// Настройки billing BFF (webhook proxy и т.д.)
/// </summary>
public class BillingOptions
{
    /// <summary>
    /// Shared secret для входящих webhook-запросов. Пустое значение — проверка отключена (dev).
    /// </summary>
    public string WebhookApiKey { get; set; } = string.Empty;
}

namespace AggregatorService.Options;

/// <summary>
/// Опции для настройки AggregatorService
/// </summary>
public class AggregatorServiceOptions
{
    /// <summary>
    /// Базовый URL VocabularyService для gRPC вызовов
    /// </summary>
    public string VocabularyServiceBaseUrl { get; set; } = "http://localhost:5117";

    /// <summary>
    /// Базовый URL authorization-module для gRPC вызовов
    /// </summary>
    public string AuthorizationModuleBaseUrl { get; set; } = "http://localhost:5027";

    /// <summary>
    /// Базовый URL MediaService для gRPC вызовов
    /// </summary>
    public string MediaServiceBaseUrl { get; set; } = "http://localhost:5121";

    /// <summary>
    /// Базовый URL AgentService для gRPC вызовов
    /// </summary>
    public string AgentServiceBaseUrl { get; set; } = "http://localhost:5131";

    /// <summary>
    /// Базовый URL BillingService для gRPC вызовов
    /// </summary>
    public string BillingServiceBaseUrl { get; set; } = "http://localhost:5127";
}

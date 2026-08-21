using System.Net.Http;

namespace AggregatorService;

/// <summary>
/// Общий HttpHandler для gRPC-клиентов: h2c к VocabularyService / authorization-module.
/// UseProxy = false — иначе на Windows часто «RequestVersionExact HTTP/2» при системном прокси/Fiddler.
/// </summary>
internal static class GrpcClientConfiguration
{
    public static SocketsHttpHandler CreateSocketsHandler() => new()
    {
        EnableMultipleHttp2Connections = true,
        UseProxy = false,
    };
}

using System.Security.Cryptography;
using System.Text;
using AggregatorService.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace AggregatorService.Filters;

/// <summary>
/// Validates <c>X-Ai-Proxy-Key</c> for editor/BFF calls to <c>/api/ai/*</c>.
/// </summary>
public sealed class AiProxyApiKeyFilter : IAsyncActionFilter
{
    public const string HeaderName = "X-Ai-Proxy-Key";

    private readonly AiCompletionOptions _opts;

    public AiProxyApiKeyFilter(IOptions<AiCompletionOptions> options)
    {
        _opts = options.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (string.IsNullOrEmpty(_opts.ProxyApiKey))
        {
            context.Result = new ObjectResult(new
            {
                error = "AI proxy is disabled: set Ai:ProxyApiKey (or Ai__ProxyApiKey).",
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var supplied) ||
            !ConstantTimeEquals(supplied.ToString(), _opts.ProxyApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = $"Send header {HeaderName} with the configured proxy secret.",
            });
            return;
        }

        await next().ConfigureAwait(false);
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}

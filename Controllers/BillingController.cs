using AggregatorService.Dtos.Billing;
using AggregatorService.Helpers;
using AggregatorService.Options;
using AggregatorService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace AggregatorService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingServiceClient _billingClient;
    private readonly BillingOptions _billingOptions;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        IBillingServiceClient billingClient,
        IOptions<BillingOptions> billingOptions,
        ILogger<BillingController> logger)
    {
        _billingClient = billingClient;
        _billingOptions = billingOptions.Value;
        _logger = logger;
    }

    [HttpGet("access")]
    [ProducesResponseType(typeof(AccessDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccessDto>> GetAccess()
    {
        var userId = MappingHelper.GetUserId(User, Request.Headers);
        var result = await _billingClient.CheckAccessAsync(userId, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("entitlements")]
    [ProducesResponseType(typeof(EntitlementsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EntitlementsDto>> GetEntitlements()
    {
        var userId = MappingHelper.GetUserId(User, Request.Headers);
        var result = await _billingClient.GetEntitlementsAsync(userId, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("usage")]
    [ProducesResponseType(typeof(BillingUsageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BillingUsageDto>> GetUsage()
    {
        var userId = MappingHelper.GetUserId(User, Request.Headers);
        var result = await _billingClient.GetUsageAsync(userId, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("subscription")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription()
    {
        var userId = MappingHelper.GetUserId(User, Request.Headers);
        var result = await _billingClient.GetSubscriptionAsync(userId, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("plans")]
    [ProducesResponseType(typeof(List<PlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PlanDto>>> ListPlans([FromQuery] bool onlyActive = true)
    {
        var result = await _billingClient.ListPlansAsync(onlyActive, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckoutResponseDto>> CreateCheckout([FromBody] CheckoutRequestDto request)
    {
        var userId = MappingHelper.GetUserId(User, Request.Headers);
        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value
            ?? string.Empty;

        var result = await _billingClient.CreateCheckoutAsync(userId, email, request, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpPost("subscription/cancel")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionDto>> CancelSubscription([FromBody] CancelSubscriptionRequestDto request)
    {
        var userId = MappingHelper.GetUserId(User, Request.Headers);
        var result = await _billingClient.CancelSubscriptionAsync(userId, request.CancelAtPeriodEnd, HttpContext.RequestAborted);

        if (result == null)
        {
            return NotFound(new { error = "No active subscription found" });
        }

        return Ok(result);
    }

    [HttpGet("invoices")]
    [ProducesResponseType(typeof(List<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<InvoiceDto>>> ListInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = MappingHelper.GetUserId(User, Request.Headers);
        var result = await _billingClient.ListInvoicesAsync(userId, page, pageSize, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpPost("webhooks/{provider}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ProcessWebhook(string provider)
    {
        if (!string.IsNullOrWhiteSpace(_billingOptions.WebhookApiKey))
        {
            var apiKey = Request.Headers["X-Billing-Webhook-Key"].FirstOrDefault();
            if (!string.Equals(apiKey, _billingOptions.WebhookApiKey, StringComparison.Ordinal))
            {
                _logger.LogWarning("Billing webhook rejected: invalid or missing X-Billing-Webhook-Key");
                return Unauthorized(new { error = "Invalid webhook API key" });
            }
        }

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(HttpContext.RequestAborted);
        var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault()
            ?? Request.Headers["YooKassa-Signature"].FirstOrDefault();

        _logger.LogInformation("Billing webhook received for provider {Provider}", provider);

        await _billingClient.ProcessWebhookAsync(provider, payload, signature, HttpContext.RequestAborted);
        return Ok(new { processed = true });
    }
}

using AggregatorService.Dtos.Billing;

namespace AggregatorService.Services;

public interface IBillingServiceClient
{
    Task<AccessDto> CheckAccessAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<EntitlementsDto> GetEntitlementsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<BillingUsageDto> GetUsageAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<SubscriptionDto?> GetSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<PlanDto>> ListPlansAsync(bool onlyActive, CancellationToken cancellationToken = default);

    Task<CheckoutResponseDto> CreateCheckoutAsync(
        Guid userId,
        string email,
        CheckoutRequestDto request,
        CancellationToken cancellationToken = default);

    Task<SubscriptionDto?> CancelSubscriptionAsync(
        Guid userId,
        bool cancelAtPeriodEnd,
        CancellationToken cancellationToken = default);

    Task<List<InvoiceDto>> ListInvoicesAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task ProcessWebhookAsync(
        string provider,
        string payload,
        string? signature,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, string>> GetUsersBillingStateAsync(
        List<string> userIds,
        CancellationToken cancellationToken = default);

    Task<PlanDto> UpdatePlanEntitlementsAsync(
        string planId,
        Dictionary<string, string> entitlements,
        CancellationToken cancellationToken = default);

    Task<string> AdminAssignPlanAsync(
        string userId,
        string planCode,
        CancellationToken cancellationToken = default);
}

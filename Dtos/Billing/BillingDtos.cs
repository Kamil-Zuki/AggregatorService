namespace AggregatorService.Dtos.Billing;

public record AccessDto(
    bool HasAccess,
    string PlanCode,
    string Status,
    DateTime? CurrentPeriodEnd);

public record EntitlementsDto(
    string PlanCode,
    IReadOnlyDictionary<string, string> Entitlements);

public record SubscriptionDto(
    string Id,
    string PlanCode,
    string Provider,
    string Status,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime? TrialStart,
    DateTime? TrialEnd,
    bool CancelAtPeriodEnd,
    DateTime? CanceledAt,
    DateTime CreatedAt);

public record PlanDto(
    string Id,
    string Code,
    string Name,
    string Description,
    int Price,
    string Currency,
    string Interval,
    bool IsActive,
    bool IsDefault,
    int TrialDays,
    IReadOnlyDictionary<string, string> Entitlements);

public record CheckoutRequestDto(
    string PlanCode,
    string? Provider,
    string? ReturnUrl);

public record CheckoutResponseDto(
    string CheckoutUrl,
    string ProviderPaymentId);

public record InvoiceDto(
    string Id,
    string SubscriptionId,
    string Provider,
    string ProviderInvoiceId,
    int AmountDue,
    int AmountPaid,
    string Currency,
    string Status,
    string? InvoicePdfUrl,
    DateTime? PaidAt,
    DateTime CreatedAt);

public record CancelSubscriptionRequestDto(
    bool CancelAtPeriodEnd);

public record BillingUsageItemDto(
    int Used,
    int Limit,
    bool IsUnlimited);

public record BillingUsageDto(
    string PlanCode,
    BillingUsageItemDto Projects,
    BillingUsageItemDto Cards,
    BillingUsageItemDto AiRequests,
    BillingUsageItemDto Books);

public record UserUsageStatsDto(
    int ProjectsUsed,
    int CardsUsed,
    int AiRequestsTodayUsed,
    int BooksUsed);

using AggregatorService.Dtos.Billing;
using Grpc.Core;
using Pvs.Billing.Grpc;
using Pvs.Content.Grpc;
using GrpcBillingServiceClient = Pvs.Billing.Grpc.BillingService.BillingServiceClient;

namespace AggregatorService.Services;

public class BillingServiceClient : IBillingServiceClient
{
    private readonly GrpcBillingServiceClient _grpcClient;
    private readonly IVocabularyServiceClient _vocabularyClient;
    private readonly ILogger<BillingServiceClient> _logger;

    public BillingServiceClient(
        GrpcBillingServiceClient grpcClient,
        IVocabularyServiceClient vocabularyClient,
        ILogger<BillingServiceClient> logger)
    {
        _grpcClient = grpcClient;
        _vocabularyClient = vocabularyClient;
        _logger = logger;
    }

    public async Task<AccessDto> CheckAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _grpcClient.CheckAccessAsync(
            new CheckAccessRequest { UserId = userId.ToString() },
            cancellationToken: cancellationToken);

        return new AccessDto(
            response.HasAccess,
            response.PlanCode,
            response.Status,
            response.CurrentPeriodEnd?.ToDateTime());
    }

    public async Task<EntitlementsDto> GetEntitlementsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _grpcClient.GetEntitlementsAsync(
            new GetEntitlementsRequest { UserId = userId.ToString() },
            cancellationToken: cancellationToken);

        return new EntitlementsDto(
            response.PlanCode,
            response.Entitlements.ToDictionary(
                x => x.Key,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase));
    }

    public async Task<BillingUsageDto> GetUsageAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entitlementsDto = await GetEntitlementsAsync(userId, cancellationToken);
        var planCode = entitlementsDto.PlanCode;
        var map = entitlementsDto.Entitlements;

        var projectsLimit = ParseInt(map, "maxProjects", 3);
        var cardsLimit = ParseInt(map, "maxCards", 500);
        var aiLimit = ParseInt(map, "aiRequestsPerDay", 10);
        var booksLimit = ParseInt(map, "textWorkspaceMaxBooks", 3);

        var projectsUsed = 0;
        var cardsUsed = 0;
        var aiRequestsTodayUsed = 0;
        var booksUsed = 0;

        try
        {
            var usageStats = await _vocabularyClient.GetUserUsageStatsAsync(
                new GetUserUsageStatsRequest { UserId = userId.ToString() },
                userId,
                Array.Empty<string>(),
                cancellationToken);

            projectsUsed = usageStats.ProjectsUsed;
            cardsUsed = usageStats.CardsUsed;
            aiRequestsTodayUsed = usageStats.AiRequestsTodayUsed;
            booksUsed = usageStats.BooksUsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch detailed usage stats from VocabularyService for user {UserId}", userId);
        }

        return new BillingUsageDto(
            planCode,
            new BillingUsageItemDto(projectsUsed, projectsLimit, projectsLimit < 0),
            new BillingUsageItemDto(cardsUsed, cardsLimit, cardsLimit < 0),
            new BillingUsageItemDto(aiRequestsTodayUsed, aiLimit, aiLimit < 0),
            new BillingUsageItemDto(booksUsed, booksLimit, booksLimit < 0));
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> dict, string key, int defaultValue)
    {
        if (dict.TryGetValue(key, out var val) && int.TryParse(val, out var parsed))
        {
            return parsed;
        }
        return defaultValue;
    }

    public async Task<SubscriptionDto?> GetSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _grpcClient.GetSubscriptionAsync(
            new GetSubscriptionRequest { UserId = userId.ToString() },
            cancellationToken: cancellationToken);

        return response.Subscription is null ? null : MapSubscription(response.Subscription);
    }

    public async Task<List<PlanDto>> ListPlansAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var response = await _grpcClient.ListPlansAsync(
            new ListPlansRequest { OnlyActive = onlyActive },
            cancellationToken: cancellationToken);

        return response.Plans.Select(MapPlan).ToList();
    }

    public async Task<CheckoutResponseDto> CreateCheckoutAsync(
        Guid userId,
        string email,
        CheckoutRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var grpcRequest = new CreateCheckoutRequest
        {
            UserId = userId.ToString(),
            Email = email,
            PlanCode = request.PlanCode,
            Provider = request.Provider ?? string.Empty,
            ReturnUrl = request.ReturnUrl ?? string.Empty
        };

        var response = await _grpcClient.CreateCheckoutAsync(grpcRequest, cancellationToken: cancellationToken);

        return new CheckoutResponseDto(response.CheckoutUrl, response.ProviderPaymentId);
    }

    public async Task<SubscriptionDto?> CancelSubscriptionAsync(
        Guid userId,
        bool cancelAtPeriodEnd,
        CancellationToken cancellationToken = default)
    {
        var response = await _grpcClient.CancelSubscriptionAsync(
            new CancelSubscriptionRequest
            {
                UserId = userId.ToString(),
                CancelAtPeriodEnd = cancelAtPeriodEnd
            },
            cancellationToken: cancellationToken);

        return response.Subscription is null ? null : MapSubscription(response.Subscription);
    }

    public async Task<List<InvoiceDto>> ListInvoicesAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var response = await _grpcClient.ListInvoicesAsync(
            new ListInvoicesRequest
            {
                UserId = userId.ToString(),
                Page = page,
                PageSize = pageSize
            },
            cancellationToken: cancellationToken);

        return response.Invoices.Select(MapInvoice).ToList();
    }

    public async Task ProcessWebhookAsync(
        string provider,
        string payload,
        string? signature,
        CancellationToken cancellationToken = default)
    {
        var request = new ProcessWebhookRequest
        {
            Provider = provider,
            Payload = payload,
            Signature = signature ?? string.Empty
        };

        await _grpcClient.ProcessWebhookAsync(request, cancellationToken: cancellationToken);
    }

    private static SubscriptionDto MapSubscription(Pvs.Billing.Grpc.Subscription subscription)
    {
        return new SubscriptionDto(
            subscription.Id,
            subscription.PlanCode,
            subscription.Provider,
            subscription.Status,
            subscription.CurrentPeriodStart.ToDateTime(),
            subscription.CurrentPeriodEnd.ToDateTime(),
            subscription.TrialStart?.ToDateTime(),
            subscription.TrialEnd?.ToDateTime(),
            subscription.CancelAtPeriodEnd,
            subscription.CanceledAt?.ToDateTime(),
            subscription.CreatedAt.ToDateTime());
    }

    public async Task<Dictionary<string, string>> GetUsersBillingStateAsync(
        List<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var request = new GetUsersBillingStateRequest();
        request.UserIds.AddRange(userIds);

        var response = await _grpcClient.GetUsersBillingStateAsync(request, cancellationToken: cancellationToken);

        return response.States.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PlanCode);
    }

    public async Task<PlanDto> UpdatePlanEntitlementsAsync(
        string planId,
        Dictionary<string, string> entitlements,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdatePlanEntitlementsRequest
        {
            PlanId = planId
        };
        
        foreach (var entitlement in entitlements)
        {
            request.Entitlements.Add(entitlement.Key, entitlement.Value);
        }

        var response = await _grpcClient.UpdatePlanEntitlementsAsync(request, cancellationToken: cancellationToken);

        return MapPlan(response.Plan);
    }

    private static PlanDto MapPlan(Pvs.Billing.Grpc.Plan plan)
    {
        return new PlanDto(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.Description,
            plan.Price,
            plan.Currency,
            plan.Interval,
            plan.IsActive,
            plan.IsDefault,
            plan.TrialDays,
            plan.Entitlements.ToDictionary(x => x.Key, x => x.Value));
    }

    private static InvoiceDto MapInvoice(Pvs.Billing.Grpc.Invoice invoice)
    {
        return new InvoiceDto(
            invoice.Id,
            invoice.SubscriptionId,
            invoice.Provider,
            invoice.ProviderInvoiceId,
            invoice.AmountDue,
            invoice.AmountPaid,
            invoice.Currency,
            invoice.Status,
            string.IsNullOrWhiteSpace(invoice.InvoicePdfUrl) ? null : invoice.InvoicePdfUrl,
            invoice.PaidAt?.ToDateTime(),
            invoice.CreatedAt.ToDateTime());
    }

    public async Task<string> AdminAssignPlanAsync(
        string userId,
        string planCode,
        CancellationToken cancellationToken = default)
    {
        var request = new AdminAssignPlanRequest
        {
            UserId = userId,
            PlanCode = planCode
        };

        var response = await _grpcClient.AdminAssignPlanAsync(request, cancellationToken: cancellationToken);

        return response.PlanCode;
    }
}

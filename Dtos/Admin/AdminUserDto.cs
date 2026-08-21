namespace AggregatorService.Dtos.Admin;

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RegistrationDate { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public bool IsLockedOut { get; set; }
}

public class AdminUsersResponseDto
{
    public List<AdminUserDto> Users { get; set; } = [];
    public int TotalCount { get; set; }
}

public class AdminUpdatePlanEntitlementsRequestDto
{
    public Dictionary<string, string> Entitlements { get; set; } = [];
}

public class AdminSetLockoutRequestDto
{
    public bool Lock { get; set; }
}

public class AdminAssignPlanRequestDto
{
    public string PlanCode { get; set; } = string.Empty;
}

public class AdminUserDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public bool IsLockedOut { get; set; }
    public Dictionary<string, string> Entitlements { get; set; } = [];
    public AggregatorService.Dtos.Billing.SubscriptionDto? Subscription { get; set; }
}

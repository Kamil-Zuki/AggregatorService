using AggregatorService.Dtos.Admin;
using AggregatorService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAuthorizationServiceClient _authClient;
    private readonly IBillingServiceClient _billingClient;

    public AdminController(IAuthorizationServiceClient authClient, IBillingServiceClient billingClient)
    {
        _authClient = authClient;
        _billingClient = billingClient;
    }

    [HttpGet("users")]
    public async Task<ActionResult<AdminUsersResponseDto>> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? planFilter = null)
    {
        // 1. Fetch users from Authorization Service
        var authResponse = await _authClient.GetUsersListAsync(page, pageSize, search, HttpContext.RequestAborted);
        
        // 2. Extract User IDs to fetch billing states
        var userIds = authResponse.Users.Select(u => u.Id).ToList();

        // 3. Fetch Billing States from Billing Service
        var billingStates = await _billingClient.GetUsersBillingStateAsync(userIds, HttpContext.RequestAborted);

        // 4. Combine data
        foreach (var user in authResponse.Users)
        {
            user.PlanCode = billingStates.GetValueOrDefault(user.Id, "free");
        }

        // 5. Apply plan filter if specified
        if (!string.IsNullOrWhiteSpace(planFilter) && planFilter.ToLower() != "all")
        {
            authResponse.Users = authResponse.Users
                .Where(u => u.PlanCode.Equals(planFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
            authResponse.TotalCount = authResponse.Users.Count; // Adjust count for filtered set
        }

        return Ok(authResponse);
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<AdminUserDetailDto>> GetUserDetail(string userId)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
            return BadRequest("Invalid user ID");

        // Fetch User Info
        var userInfo = await _authClient.GetUserInfoAsync(parsedUserId, HttpContext.RequestAborted);

        // Fetch Billing Entitlements & Subscription
        var entitlements = await _billingClient.GetEntitlementsAsync(parsedUserId, HttpContext.RequestAborted);
        var subscription = await _billingClient.GetSubscriptionAsync(parsedUserId, HttpContext.RequestAborted);
        
        // Fetch users list with search by email to get IsLockedOut (since GetUserInfo doesn't return it)
        var usersList = await _authClient.GetUsersListAsync(1, 10, userInfo.Email, HttpContext.RequestAborted);
        var userInList = usersList.Users.FirstOrDefault(u => u.Id == userId);
        
        var detail = new AdminUserDetailDto
        {
            Id = userInfo.Id.ToString(),
            UserName = userInfo.UserName,
            Email = userInfo.Email,
            AvatarUrl = userInfo.AvatarUrl ?? string.Empty,
            PlanCode = entitlements.PlanCode,
            IsLockedOut = userInList?.IsLockedOut ?? false,
            Entitlements = entitlements.Entitlements.ToDictionary(k => k.Key, v => v.Value),
            Subscription = subscription
        };

        return Ok(detail);
    }

    [HttpPut("users/{userId}/lockout")]
    public async Task<ActionResult> SetUserLockout(string userId, [FromBody] AdminSetLockoutRequestDto request)
    {
        var result = await _authClient.AdminSetUserLockoutAsync(userId, request.Lock, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpPut("users/{userId}/plan")]
    public async Task<ActionResult> AssignPlan(string userId, [FromBody] AdminAssignPlanRequestDto request)
    {
        var assignedPlanCode = await _billingClient.AdminAssignPlanAsync(userId, request.PlanCode, HttpContext.RequestAborted);
        return Ok(new { PlanCode = assignedPlanCode });
    }

    [HttpPut("plans/{planId}/entitlements")]
    public async Task<ActionResult> UpdatePlanEntitlements(string planId, [FromBody] AdminUpdatePlanEntitlementsRequestDto request)
    {
        var updatedPlan = await _billingClient.UpdatePlanEntitlementsAsync(planId, request.Entitlements, HttpContext.RequestAborted);
        return Ok(updatedPlan);
    }
}

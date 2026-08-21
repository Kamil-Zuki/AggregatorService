using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace AggregatorService.Helpers;

public static class BillingLimitHttp
{
    public static bool TryHandleRpcException(RpcException ex, out ObjectResult result)
    {
        if (ex.StatusCode == StatusCode.ResourceExhausted && ex.Status.Detail.StartsWith("Billing limit exceeded:"))
        {
            var limitKey = ex.Status.Detail.Split(':')[1].Trim();
            
            result = new ObjectResult(new
            {
                code = "BILLING_LIMIT_EXCEEDED",
                limitKey = limitKey,
                message = ex.Status.Detail
            })
            {
                StatusCode = StatusCodes.Status402PaymentRequired
            };
            return true;
        }

        result = null!;
        return false;
    }
}

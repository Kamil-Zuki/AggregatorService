namespace AggregatorService.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using AggregatorService.Options;

public class FeatureFlagFilterAttribute : ActionFilterAttribute
{
    private readonly string _featureName;

    public FeatureFlagFilterAttribute(string featureName)
    {
        _featureName = featureName;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var options = context.HttpContext.RequestServices.GetService<IOptionsSnapshot<FeaturesOptions>>();
        if (options == null) return;

        bool isEnabled = _featureName switch
        {
            "EnableAIAgents" => options.Value.EnableAIAgents,
            "EnableAdvancedModules" => options.Value.EnableAdvancedModules,
            _ => true
        };

        if (!isEnabled)
        {
            context.Result = new NotFoundObjectResult(new { error = "Feature is disabled" });
        }
    }
}

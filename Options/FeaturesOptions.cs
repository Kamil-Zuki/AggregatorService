namespace AggregatorService.Options;

public class FeaturesOptions
{
    public const string SectionName = "Features";

    public bool EnableAIAgents { get; set; } = false;
    public bool EnableAdvancedModules { get; set; } = false;
}

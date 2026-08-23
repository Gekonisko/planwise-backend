namespace PlanWise.Modules.CostEstimation.Infrastructure.Llm;

public sealed class AnthropicOptions
{
    public const string SectionName = "CostEstimation:Anthropic";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "claude-sonnet-5";

    public string BaseUrl { get; init; } = "https://api.anthropic.com";
}

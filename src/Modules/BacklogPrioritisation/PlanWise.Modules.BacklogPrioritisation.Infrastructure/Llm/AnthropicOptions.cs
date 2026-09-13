namespace PlanWise.Modules.BacklogPrioritisation.Infrastructure.Llm;

// Deliberately a separate section from CostEstimation's: modules configure themselves, and the two
// jobs have genuinely different needs — prioritising a backlog is a judgement call over short text,
// costing a job is long-form reasoning over money — so each should be free to pick its own model
// without the other's config changing underneath it.
public sealed class AnthropicOptions
{
    public const string SectionName = "BacklogPrioritisation:Anthropic";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "claude-opus-5";

    public string BaseUrl { get; init; } = "https://api.anthropic.com";
}

namespace PlanWise.Modules.Scheduling.Infrastructure.Llm;

public sealed class AnthropicSchedulingOptions
{
    public const string SectionName = "Scheduling:Anthropic";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "claude-opus-5";

    public string BaseUrl { get; init; } = "https://api.anthropic.com";

    /// <summary>
    /// How many assistant turns the agent may take before it is forced to commit. Every turn is a
    /// billed request, and the loop is the whole point of the agent, so this is the single knob that
    /// trades money for solution quality — and the variable the evaluation sweeps.
    /// </summary>
    public int MaxTurns { get; init; } = 6;
}

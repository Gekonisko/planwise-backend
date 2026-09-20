namespace PlanWise.Modules.CostEstimation.Infrastructure.Llm;

public sealed class AnthropicOptions
{
    public const string SectionName = "CostEstimation:Anthropic";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "claude-sonnet-5";

    public string BaseUrl { get; init; } = "https://api.anthropic.com";

    /// <summary>
    /// Read only by AgenticCostEstimationModel: how many turns it may take before it is forced to
    /// submit. One turn withholds the checking tool entirely and reproduces the single-shot model,
    /// which is what makes the two arms of the study comparable. Ignored by
    /// AnthropicCostEstimationModel, which has no loop to budget.
    /// </summary>
    public int MaxTurns { get; init; } = 4;
}

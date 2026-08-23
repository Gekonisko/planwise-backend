using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Abstractions;

public sealed record RoleRate(string Role, decimal HourlyRate, string Currency);

public sealed record CostEstimationPrompt(
    string ProjectName,
    string? ClientName,
    string Currency,
    IReadOnlyList<CostEstimationTaskSummary> Tasks,
    IReadOnlyList<RoleRate> RateCard);

// The LLM client boundary: Application depends only on this interface, never on the HTTP/Anthropic
// specifics, which live in Infrastructure (AnthropicCostEstimationModel). Keeping the prompt/result
// shape here (not string-typed) means swapping providers later only touches Infrastructure.
public interface ICostEstimationModel
{
    string ModelName { get; }

    Task<CostEstimateResult> EstimateAsync(CostEstimationPrompt prompt, CancellationToken cancellationToken = default);
}

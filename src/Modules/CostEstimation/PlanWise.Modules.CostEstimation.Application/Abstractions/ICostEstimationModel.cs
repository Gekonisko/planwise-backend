using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Abstractions;

/// <param name="HourlyRate">Capacity-weighted blend of the rates of everyone in this role.</param>
/// <param name="Headcount">
/// Sum of the role's capacity fractions (2.5 = e.g. two full-time plus one half-time), which is what
/// bounds how many hours the role can realistically absorb.
/// </param>
/// <param name="Members">
/// The people this line is blended from. Carried through so a reader can see why the rate is what it
/// is, rather than having to re-derive the grouping and weighting rules client-side.
/// </param>
public sealed record RoleRate(
    string Role,
    decimal HourlyRate,
    decimal Headcount,
    string Currency,
    IReadOnlyList<RateCardMember> Members);

public sealed record RateCardMember(string Email, string Role, decimal Capacity, decimal HourlyRate);

public sealed record CostEstimationPrompt(
    string ProjectName,
    string? ClientName,
    string Currency,
    IReadOnlyList<CostEstimationTaskSummary> Tasks,
    IReadOnlyList<RoleRate> RateCard,
    bool RateCardIsFromProjectTeam);

// The LLM client boundary: Application depends only on this interface, never on the HTTP/Anthropic
// specifics, which live in Infrastructure (AnthropicCostEstimationModel). Keeping the prompt/result
// shape here (not string-typed) means swapping providers later only touches Infrastructure.
public interface ICostEstimationModel
{
    string ModelName { get; }

    Task<CostEstimateResult> EstimateAsync(CostEstimationPrompt prompt, CancellationToken cancellationToken = default);
}

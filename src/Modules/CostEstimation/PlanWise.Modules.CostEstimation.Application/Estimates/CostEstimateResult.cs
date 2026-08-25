namespace PlanWise.Modules.CostEstimation.Application.Estimates;

public sealed record CostScenario(string Name, int Percentile, decimal Total, string Confidence);

public sealed record LabourLine(string Role, decimal Hours, decimal HourlyRate, decimal Cost);

public sealed record NonLabourLine(string Description, decimal Amount);

public sealed record PriorityCostLine(string Priority, decimal Total);

public sealed record CostReduction(Guid Id, string Description, decimal Saving, string Effect, string Confidence);

// The full structured output of a cost-estimate model run. "Epic breakdown" from the spec is
// represented as PriorityBreakdown instead — there is no epic/grouping concept above individual
// tasks anywhere in the codebase (same gap flagged for the Scheduling Gantt), so priority is the
// closest existing dimension to break costs down by. Reductions.Id is assigned locally when parsing
// the model response (see AnthropicCostEstimationModel), not by the model itself — LLMs are
// unreliable at producing valid, stable identifiers, so the model is only asked for the
// description/saving/effect/confidence and a fresh Guid is generated per item.
public sealed record CostEstimateResult(
    IReadOnlyList<CostScenario> Scenarios,
    IReadOnlyList<LabourLine> LabourLines,
    IReadOnlyList<NonLabourLine> NonLabourLines,
    IReadOnlyList<PriorityCostLine> PriorityBreakdown,
    IReadOnlyList<string> Assumptions,
    string Reasoning,
    IReadOnlyList<CostReduction> Reductions);

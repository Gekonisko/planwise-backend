namespace PlanWise.Modules.CostEstimation.Application.Estimates.Reductions;

public sealed record ReductionResponse(Guid Id, string Description, decimal Saving, string Effect, string Confidence, bool Applied);

public sealed record ReductionsResponse(
    Guid RunId,
    string Currency,
    decimal BaselineTotal,
    decimal ProjectedTotal,
    IReadOnlyList<ReductionResponse> Reductions);

namespace PlanWise.Modules.CostEstimation.Application.Estimates;

public sealed record CostEstimateResponse(
    Guid Id,
    Guid ProjectId,
    Guid JobId,
    string ModelName,
    string Currency,
    CostEstimateResult Result,
    DateTime CreatedAtUtc);

public sealed record CostEstimateExplanationResponse(
    Guid Id,
    string ModelName,
    IReadOnlyList<string> Assumptions,
    string Reasoning,
    DateTime GeneratedAtUtc);

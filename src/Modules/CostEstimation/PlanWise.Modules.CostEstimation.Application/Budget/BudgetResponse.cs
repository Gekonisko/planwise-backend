namespace PlanWise.Modules.CostEstimation.Application.Budget;

public sealed record BudgetResponse(Guid ProjectId, decimal Amount, string Currency, DateTime? UpdatedAtUtc);

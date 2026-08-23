using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.GetCostEstimateHistory;

public sealed record GetCostEstimateHistoryQuery(Guid ProjectId) : IQuery<IReadOnlyList<CostEstimateResponse>>;

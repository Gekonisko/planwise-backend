using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.GetLatestCostEstimate;

public sealed record GetLatestCostEstimateQuery(Guid ProjectId) : IQuery<CostEstimateResponse>;

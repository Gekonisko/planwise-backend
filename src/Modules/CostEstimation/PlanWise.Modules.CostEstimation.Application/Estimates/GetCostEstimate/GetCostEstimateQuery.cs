using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.GetCostEstimate;

public sealed record GetCostEstimateQuery(Guid RunId) : IQuery<CostEstimateResponse>;

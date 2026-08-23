using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.GetCostEstimateExplanation;

public sealed record GetCostEstimateExplanationQuery(Guid RunId) : IQuery<CostEstimateExplanationResponse>;

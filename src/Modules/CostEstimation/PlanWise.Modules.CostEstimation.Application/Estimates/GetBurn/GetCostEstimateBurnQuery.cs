using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.GetBurn;

public sealed record GetCostEstimateBurnQuery(Guid RunId) : IQuery<BurnResponse>;

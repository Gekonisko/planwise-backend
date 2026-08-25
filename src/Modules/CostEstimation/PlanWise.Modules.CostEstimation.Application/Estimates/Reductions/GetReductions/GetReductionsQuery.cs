using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.CostEstimation.Application.Estimates.Reductions;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.Reductions.GetReductions;

public sealed record GetReductionsQuery(Guid RunId) : IQuery<ReductionsResponse>;

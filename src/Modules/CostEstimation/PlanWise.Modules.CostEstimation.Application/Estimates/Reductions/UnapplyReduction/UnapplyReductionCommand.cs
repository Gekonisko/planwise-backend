using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.CostEstimation.Application.Estimates.Reductions;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.Reductions.UnapplyReduction;

public sealed record UnapplyReductionCommand(Guid RunId, Guid ReductionId) : ICommand<ReductionsResponse>;

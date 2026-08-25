using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.CostEstimation.Application.Estimates.Reductions;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.Reductions.ApplyReduction;

public sealed record ApplyReductionCommand(Guid RunId, Guid ReductionId) : ICommand<ReductionsResponse>;

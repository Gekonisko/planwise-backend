using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.RunCostEstimate;

public sealed record RunCostEstimateCommand(Guid ProjectId) : ICommand<Guid>;

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.OptimiseSchedule;

public sealed record OptimiseScheduleCommand(Guid ProjectId) : ICommand<Guid>;

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities.RunPriorities;

public sealed record RunPrioritiesCommand(Guid ProjectId) : ICommand<Guid>;

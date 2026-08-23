using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.BacklogPrioritisation.Application.Priorities;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities.ApplyPriorities;

public sealed record ApplyPrioritiesCommand(Guid ProjectId) : ICommand<PrioritiesResponse>;

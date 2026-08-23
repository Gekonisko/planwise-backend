using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities.DismissPriorities;

public sealed record DismissPrioritiesCommand(Guid ProjectId, string? Reason) : ICommand;

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities.GetPriorities;

public sealed record GetPrioritiesQuery(Guid ProjectId) : IQuery<PrioritiesResponse>;

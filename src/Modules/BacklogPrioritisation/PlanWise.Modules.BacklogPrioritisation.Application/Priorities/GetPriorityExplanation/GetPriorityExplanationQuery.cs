using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities.GetPriorityExplanation;

public sealed record GetPriorityExplanationQuery(Guid Id) : IQuery<PriorityExplanationResponse>;

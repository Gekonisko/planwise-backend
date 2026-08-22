using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints.GetSprint;

public sealed record GetSprintQuery(Guid SprintId) : IQuery<SprintResponse>;

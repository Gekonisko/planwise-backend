using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints.StartSprint;

public sealed record StartSprintCommand(Guid SprintId) : ICommand<SprintResponse>;

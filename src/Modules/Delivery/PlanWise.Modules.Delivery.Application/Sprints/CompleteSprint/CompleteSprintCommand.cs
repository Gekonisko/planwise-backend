using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints.CompleteSprint;

public sealed record CompleteSprintCommand(Guid SprintId) : ICommand<SprintResponse>;

using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints.UpdateSprint;

public sealed record UpdateSprintCommand(
    Guid SprintId,
    string? Name,
    string? Goal,
    DateOnly? StartDate,
    DateOnly? EndDate) : ICommand<SprintResponse>;

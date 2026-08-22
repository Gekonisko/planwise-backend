using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints.CreateSprint;

public sealed record CreateSprintCommand(
    Guid ProjectId,
    string Name,
    string? Goal,
    DateOnly StartDate,
    DateOnly EndDate) : ICommand<SprintResponse>;

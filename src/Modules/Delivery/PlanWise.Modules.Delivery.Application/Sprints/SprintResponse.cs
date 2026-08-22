using PlanWise.Modules.Delivery.Domain.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints;

public sealed record SprintResponse(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Goal,
    DateOnly StartDate,
    DateOnly EndDate,
    SprintState State,
    int CommittedPoints,
    int CompletedPoints);

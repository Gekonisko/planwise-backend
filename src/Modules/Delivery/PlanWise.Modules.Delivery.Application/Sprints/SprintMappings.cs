using PlanWise.Modules.Delivery.Domain.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints;

internal static class SprintMappings
{
    // Committed/completed points are always 0 until the Task entity (board and backlog) lands and
    // sprints can accumulate points from the tasks assigned to them.
    public static SprintResponse ToResponse(Sprint sprint) =>
        new(sprint.Id, sprint.ProjectId, sprint.Name, sprint.Goal, sprint.StartDate, sprint.EndDate, sprint.State, 0, 0);
}

using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Sprints;

internal static class SprintMappings
{
    public static SprintResponse ToResponse(Sprint sprint, IReadOnlyList<ProjectTask> sprintTasks) =>
        new(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate,
            sprint.EndDate,
            sprint.State,
            sprintTasks.Sum(task => task.Points ?? 0),
            sprintTasks.Where(task => task.Status == ProjectTaskStatus.Done).Sum(task => task.Points ?? 0));
}

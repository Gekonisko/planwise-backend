using PlanWise.Modules.Scheduling.Domain.Milestones;

namespace PlanWise.Modules.Scheduling.Application.Milestones;

internal static class MilestoneMappings
{
    public static MilestoneResponse ToResponse(Milestone milestone, DateOnly today) =>
        new(milestone.Id, milestone.ProjectId, milestone.Name, milestone.DueDate, milestone.DueDate < today ? "Achieved" : "Upcoming");
}

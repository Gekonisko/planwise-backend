using PlanWise.Common.Domain;

namespace PlanWise.Modules.Scheduling.Domain.Milestones;

public sealed class Milestone : Entity
{
    private Milestone()
    {
    }

    private Milestone(Guid projectId, string name, DateOnly dueDate)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Name = name;
        DueDate = dueDate;
    }

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }
    public DateOnly DueDate { get; private set; }

    public static Milestone Create(Guid projectId, string name, DateOnly dueDate) =>
        new(projectId, name, dueDate);
}

using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Activity;

public sealed class ActivityLogEntry : Entity
{
    private ActivityLogEntry()
    {
    }

    private ActivityLogEntry(Guid projectId, string description, DateTime occurredAtUtc)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Description = description;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid ProjectId { get; private set; }
    public string Description { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    public static ActivityLogEntry Create(Guid projectId, string description, DateTime occurredAtUtc) =>
        new(projectId, description, occurredAtUtc);
}

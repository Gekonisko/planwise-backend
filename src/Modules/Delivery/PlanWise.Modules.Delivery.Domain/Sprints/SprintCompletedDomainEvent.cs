using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Sprints;

public sealed class SprintCompletedDomainEvent(Guid sprintId, Guid projectId, string name) : DomainEvent
{
    public Guid SprintId { get; } = sprintId;
    public Guid ProjectId { get; } = projectId;
    public string Name { get; } = name;
}

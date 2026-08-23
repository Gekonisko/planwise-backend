using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Tasks;

public sealed class ProjectTaskUpdatedDomainEvent(Guid taskId, Guid projectId, string key) : DomainEvent
{
    public Guid TaskId { get; } = taskId;
    public Guid ProjectId { get; } = projectId;
    public string Key { get; } = key;
}

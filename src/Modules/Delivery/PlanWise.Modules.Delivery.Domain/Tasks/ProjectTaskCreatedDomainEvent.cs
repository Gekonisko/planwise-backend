using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Tasks;

public sealed class ProjectTaskCreatedDomainEvent(Guid taskId, Guid projectId, string key, string title) : DomainEvent
{
    public Guid TaskId { get; } = taskId;
    public Guid ProjectId { get; } = projectId;
    public string Key { get; } = key;
    public string Title { get; } = title;
}

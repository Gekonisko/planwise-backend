using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Tasks;

public sealed class ProjectTaskMovedDomainEvent(
    Guid taskId,
    Guid projectId,
    string key,
    string title,
    ProjectTaskStatus fromStatus,
    ProjectTaskStatus toStatus) : DomainEvent
{
    public Guid TaskId { get; } = taskId;
    public Guid ProjectId { get; } = projectId;
    public string Key { get; } = key;
    public string Title { get; } = title;
    public ProjectTaskStatus FromStatus { get; } = fromStatus;
    public ProjectTaskStatus ToStatus { get; } = toStatus;
}

using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Tasks;

public sealed class ProjectTaskCommentAddedDomainEvent(Guid taskId, Guid projectId, string key, Guid authorUserId, string body) : DomainEvent
{
    public Guid TaskId { get; } = taskId;
    public Guid ProjectId { get; } = projectId;
    public string Key { get; } = key;
    public Guid AuthorUserId { get; } = authorUserId;
    public string Body { get; } = body;
}

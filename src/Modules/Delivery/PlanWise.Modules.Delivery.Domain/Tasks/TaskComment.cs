using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Tasks;

public sealed class TaskComment : Entity
{
    private TaskComment()
    {
    }

    private TaskComment(Guid taskId, Guid authorUserId, string body, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        TaskId = taskId;
        AuthorUserId = authorUserId;
        Body = body;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid TaskId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Body { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static TaskComment Create(Guid taskId, Guid authorUserId, string body, DateTime createdAtUtc) =>
        new(taskId, authorUserId, body, createdAtUtc);
}

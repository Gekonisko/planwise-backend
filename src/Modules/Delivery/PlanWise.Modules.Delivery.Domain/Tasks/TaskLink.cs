using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Tasks;

public sealed class TaskLink : Entity
{
    private TaskLink()
    {
    }

    private TaskLink(Guid taskId, Guid linkedTaskId, TaskLinkType type)
    {
        Id = Guid.NewGuid();
        TaskId = taskId;
        LinkedTaskId = linkedTaskId;
        Type = type;
    }

    public Guid TaskId { get; private set; }
    public Guid LinkedTaskId { get; private set; }
    public TaskLinkType Type { get; private set; }

    public static TaskLink Create(Guid taskId, Guid linkedTaskId, TaskLinkType type) =>
        new(taskId, linkedTaskId, type);
}

public enum TaskLinkType
{
    Blocks,
    BlockedBy,
    RelatesTo
}

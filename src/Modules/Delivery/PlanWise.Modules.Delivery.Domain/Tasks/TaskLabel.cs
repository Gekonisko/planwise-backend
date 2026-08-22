using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Tasks;

public sealed class TaskLabel : Entity
{
    private TaskLabel()
    {
    }

    private TaskLabel(Guid taskId, Guid labelId)
    {
        Id = Guid.NewGuid();
        TaskId = taskId;
        LabelId = labelId;
    }

    public Guid TaskId { get; private set; }
    public Guid LabelId { get; private set; }

    public static TaskLabel Create(Guid taskId, Guid labelId) => new(taskId, labelId);
}

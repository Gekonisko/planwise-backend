using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Tasks;

public sealed class Subtask : Entity
{
    private Subtask()
    {
    }

    private Subtask(Guid taskId, string title)
    {
        Id = Guid.NewGuid();
        TaskId = taskId;
        Title = title;
        IsDone = false;
    }

    public Guid TaskId { get; private set; }
    public string Title { get; private set; }
    public bool IsDone { get; private set; }

    public static Subtask Create(Guid taskId, string title) => new(taskId, title);

    public void Update(string? title, bool? isDone)
    {
        if (title is not null)
        {
            Title = title;
        }

        if (isDone is not null)
        {
            IsDone = isDone.Value;
        }
    }
}

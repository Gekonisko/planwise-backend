using PlanWise.Common.Domain;

namespace PlanWise.Modules.Scheduling.Domain.Schedule;

// One row per task that has been manually scheduled (dragged in the Gantt). Its Id is deliberately
// the same as the task's own id: tasks without a row here still appear on the Gantt using computed
// (default) dates, so the client always has a stable "schedule item id" to PATCH against, whether or
// not an override has been created yet.
public sealed class ScheduleItem : Entity
{
    private ScheduleItem()
    {
    }

    private ScheduleItem(Guid taskId, Guid projectId, DateOnly startDate, DateOnly endDate)
    {
        Id = taskId;
        ProjectId = projectId;
        TaskId = taskId;
        StartDate = startDate;
        EndDate = endDate;
    }

    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    public static ScheduleItem Create(Guid taskId, Guid projectId, DateOnly startDate, DateOnly endDate) =>
        new(taskId, projectId, startDate, endDate);

    public void Reschedule(DateOnly startDate, DateOnly endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}

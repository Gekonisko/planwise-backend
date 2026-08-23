namespace PlanWise.Common.Application.Abstractions;

public interface IProjectTasksService
{
    Task<IReadOnlyList<ScheduleTaskSummary>> GetScheduleTasksAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<ScheduleTaskSummary?> GetScheduleTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<bool> AssignTaskAsync(Guid taskId, Guid assigneeId, CancellationToken cancellationToken = default);
}

public sealed record ScheduleTaskSummary(
    Guid TaskId,
    Guid ProjectId,
    string Key,
    string Title,
    bool IsDone,
    int? Points,
    DateOnly? DueDate,
    IReadOnlyList<Guid> PredecessorTaskIds,
    Guid? AssigneeId);

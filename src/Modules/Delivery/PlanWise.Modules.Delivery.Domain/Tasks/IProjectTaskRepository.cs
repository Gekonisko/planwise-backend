namespace PlanWise.Modules.Delivery.Domain.Tasks;

public interface IProjectTaskRepository
{
    Task<ProjectTask?> GetAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectTask>> GetByIdsAsync(Guid projectId, IReadOnlyList<Guid> taskIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectTask>> GetByProjectAsync(
        Guid projectId,
        Guid? sprintId,
        ProjectTaskStatus? status,
        Guid? assigneeId,
        Guid? labelId,
        string? searchText,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectTask>> GetByStatusAsync(Guid projectId, ProjectTaskStatus status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectTask>> GetDueBetweenAsync(Guid projectId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<decimal> GetMaxRankAsync(Guid projectId, ProjectTaskStatus status, CancellationToken cancellationToken = default);

    Task<int> GetNextTaskNumberAsync(Guid projectId, CancellationToken cancellationToken = default);

    void Add(ProjectTask task);

    void Remove(ProjectTask task);

    void AddSubtask(Subtask subtask);

    void AddComment(TaskComment comment);

    void AddLink(TaskLink link);

    void AddLabels(IEnumerable<TaskLabel> labels);
}

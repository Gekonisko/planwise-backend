namespace PlanWise.Modules.Delivery.Domain.Sprints;

public interface ISprintRepository
{
    Task<Sprint?> GetAsync(Guid sprintId, CancellationToken cancellationToken = default);
    Task<Sprint?> GetForProjectAsync(Guid sprintId, Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sprint>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveSprintAsync(Guid projectId, CancellationToken cancellationToken = default);
    void Add(Sprint sprint);
}

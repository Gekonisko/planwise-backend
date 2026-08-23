namespace PlanWise.Modules.Delivery.Domain.Activity;

public interface IActivityLogRepository
{
    Task<IReadOnlyList<ActivityLogEntry>> GetByProjectAsync(
        Guid projectId, int limit, int offset, CancellationToken cancellationToken = default);

    void Add(ActivityLogEntry entry);
}

namespace PlanWise.Modules.Scheduling.Domain.Schedule;

public interface IScheduleItemRepository
{
    Task<ScheduleItem?> GetAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleItem>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    void Add(ScheduleItem item);
}

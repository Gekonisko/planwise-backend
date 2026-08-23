using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.Scheduling.Domain.Schedule;
using PlanWise.Modules.Scheduling.Infrastructure.Database;

namespace PlanWise.Modules.Scheduling.Infrastructure.Schedule;

internal sealed class ScheduleItemRepository(SchedulingDbContext dbContext) : IScheduleItemRepository
{
    public Task<ScheduleItem?> GetAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        dbContext.ScheduleItems.SingleOrDefaultAsync(item => item.TaskId == taskId, cancellationToken);

    public async Task<IReadOnlyList<ScheduleItem>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await dbContext.ScheduleItems
            .Where(item => item.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    public void Add(ScheduleItem item) => dbContext.ScheduleItems.Add(item);
}

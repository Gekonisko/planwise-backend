using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.Delivery.Domain.Activity;
using PlanWise.Modules.Delivery.Infrastructure.Database;

namespace PlanWise.Modules.Delivery.Infrastructure.Activity;

internal sealed class ActivityLogRepository(DeliveryDbContext dbContext) : IActivityLogRepository
{
    public async Task<IReadOnlyList<ActivityLogEntry>> GetByProjectAsync(
        Guid projectId, int limit, int offset, CancellationToken cancellationToken = default) =>
        await dbContext.ActivityLogEntries
            .Where(entry => entry.ProjectId == projectId)
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Add(ActivityLogEntry entry) => dbContext.ActivityLogEntries.Add(entry);
}

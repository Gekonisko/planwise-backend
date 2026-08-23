using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.Scheduling.Domain.Milestones;
using PlanWise.Modules.Scheduling.Infrastructure.Database;

namespace PlanWise.Modules.Scheduling.Infrastructure.Milestones;

internal sealed class MilestoneRepository(SchedulingDbContext dbContext) : IMilestoneRepository
{
    public async Task<IReadOnlyList<Milestone>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await dbContext.Milestones
            .Where(milestone => milestone.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    public void Add(Milestone milestone) => dbContext.Milestones.Add(milestone);
}

using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;
using PlanWise.Modules.BacklogPrioritisation.Infrastructure.Database;

namespace PlanWise.Modules.BacklogPrioritisation.Infrastructure.Priorities;

internal sealed class PriorityRunRepository(BacklogPrioritisationDbContext dbContext) : IPriorityRunRepository
{
    public Task<PriorityRun?> GetAsync(Guid runId, CancellationToken cancellationToken = default) =>
        dbContext.PriorityRuns
            .Include(run => run.Items)
            .SingleOrDefaultAsync(run => run.Id == runId, cancellationToken);

    public Task<PriorityRun?> GetLatestForProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        dbContext.PriorityRuns
            .Include(run => run.Items)
            .Where(run => run.ProjectId == projectId)
            .OrderByDescending(run => run.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(PriorityRun run) => dbContext.PriorityRuns.Add(run);
}

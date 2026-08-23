using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.CostEstimation.Domain.Estimates;
using PlanWise.Modules.CostEstimation.Infrastructure.Database;

namespace PlanWise.Modules.CostEstimation.Infrastructure.Estimates;

internal sealed class CostEstimateRunRepository(CostEstimationDbContext dbContext) : ICostEstimateRunRepository
{
    public Task<CostEstimateRun?> GetAsync(Guid runId, CancellationToken cancellationToken = default) =>
        dbContext.CostEstimateRuns.SingleOrDefaultAsync(run => run.Id == runId, cancellationToken);

    public Task<CostEstimateRun?> GetLatestForProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        dbContext.CostEstimateRuns
            .Where(run => run.ProjectId == projectId)
            .OrderByDescending(run => run.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CostEstimateRun>> GetHistoryForProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await dbContext.CostEstimateRuns
            .Where(run => run.ProjectId == projectId)
            .OrderByDescending(run => run.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(CostEstimateRun run) => dbContext.CostEstimateRuns.Add(run);
}

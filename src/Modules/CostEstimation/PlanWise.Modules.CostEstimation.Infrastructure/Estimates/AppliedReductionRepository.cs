using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.CostEstimation.Domain.Estimates;
using PlanWise.Modules.CostEstimation.Infrastructure.Database;

namespace PlanWise.Modules.CostEstimation.Infrastructure.Estimates;

internal sealed class AppliedReductionRepository(CostEstimationDbContext dbContext) : IAppliedReductionRepository
{
    public async Task<IReadOnlyList<AppliedReduction>> GetForRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
        await dbContext.AppliedReductions
            .Where(applied => applied.RunId == runId)
            .ToListAsync(cancellationToken);

    public Task<AppliedReduction?> GetAsync(Guid runId, Guid reductionId, CancellationToken cancellationToken = default) =>
        dbContext.AppliedReductions
            .SingleOrDefaultAsync(applied => applied.RunId == runId && applied.ReductionId == reductionId, cancellationToken);

    public void Add(AppliedReduction appliedReduction) => dbContext.AppliedReductions.Add(appliedReduction);

    public void Remove(AppliedReduction appliedReduction) => dbContext.AppliedReductions.Remove(appliedReduction);
}

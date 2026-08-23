using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.RiskPrediction.Domain.Risks;
using PlanWise.Modules.RiskPrediction.Infrastructure.Database;

namespace PlanWise.Modules.RiskPrediction.Infrastructure.Risks;

internal sealed class RiskAssessmentRunRepository(RiskPredictionDbContext dbContext) : IRiskAssessmentRunRepository
{
    public Task<RiskAssessmentRun?> GetAsync(Guid runId, CancellationToken cancellationToken = default) =>
        dbContext.RiskAssessmentRuns.SingleOrDefaultAsync(run => run.Id == runId, cancellationToken);

    public Task<RiskAssessmentRun?> GetLatestForProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        dbContext.RiskAssessmentRuns
            .Where(run => run.ProjectId == projectId)
            .OrderByDescending(run => run.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(RiskAssessmentRun run) => dbContext.RiskAssessmentRuns.Add(run);
}

using Microsoft.EntityFrameworkCore;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.RiskPrediction.Domain.Risks;
using PlanWise.Modules.RiskPrediction.Infrastructure.Database;

namespace PlanWise.Modules.RiskPrediction.Infrastructure.Risks;

internal sealed class RiskInsightsService(RiskPredictionDbContext dbContext) : IRiskInsightsService
{
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetLatestRiskScoresAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        RiskAssessmentRun? latestRun = await dbContext.RiskAssessmentRuns
            .Where(run => run.ProjectId == projectId)
            .OrderByDescending(run => run.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestRun is null)
        {
            return new Dictionary<Guid, decimal>();
        }

        List<TaskRiskAssessment> assessments = await dbContext.TaskRiskAssessments
            .Where(assessment => assessment.RunId == latestRun.Id)
            .ToListAsync(cancellationToken);

        return assessments.ToDictionary(assessment => assessment.TaskId, assessment => assessment.ProbabilityOfSlip);
    }
}

using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.RiskPrediction.Domain.Risks;
using PlanWise.Modules.RiskPrediction.Infrastructure.Database;

namespace PlanWise.Modules.RiskPrediction.Infrastructure.Risks;

internal sealed class TaskRiskAssessmentRepository(RiskPredictionDbContext dbContext) : ITaskRiskAssessmentRepository
{
    public Task<TaskRiskAssessment?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.TaskRiskAssessments.SingleOrDefaultAsync(assessment => assessment.Id == id, cancellationToken);

    public Task<TaskRiskAssessment?> GetLatestForTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        dbContext.TaskRiskAssessments
            .Where(assessment => assessment.TaskId == taskId)
            .OrderByDescending(assessment => assessment.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskRiskAssessment>> GetForRunAsync(Guid runId, bool excludeDismissed, CancellationToken cancellationToken = default)
    {
        IQueryable<TaskRiskAssessment> query = dbContext.TaskRiskAssessments.Where(assessment => assessment.RunId == runId);
        if (excludeDismissed)
        {
            query = query.Where(assessment => !assessment.Dismissed);
        }

        return await query.OrderByDescending(assessment => assessment.ProbabilityOfSlip).ToListAsync(cancellationToken);
    }

    public void AddRange(IEnumerable<TaskRiskAssessment> assessments) => dbContext.TaskRiskAssessments.AddRange(assessments);
}

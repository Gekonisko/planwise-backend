namespace PlanWise.Modules.RiskPrediction.Domain.Risks;

public interface IRiskAssessmentRunRepository
{
    Task<RiskAssessmentRun?> GetAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<RiskAssessmentRun?> GetLatestForProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    void Add(RiskAssessmentRun run);
}

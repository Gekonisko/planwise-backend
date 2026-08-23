namespace PlanWise.Modules.RiskPrediction.Domain.Risks;

public interface ITaskRiskAssessmentRepository
{
    Task<TaskRiskAssessment?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaskRiskAssessment?> GetLatestForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskRiskAssessment>> GetForRunAsync(Guid runId, bool excludeDismissed, CancellationToken cancellationToken = default);

    void AddRange(IEnumerable<TaskRiskAssessment> assessments);
}

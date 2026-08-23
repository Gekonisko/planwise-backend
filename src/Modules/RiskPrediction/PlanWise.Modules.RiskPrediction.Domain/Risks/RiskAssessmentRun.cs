using PlanWise.Common.Domain;

namespace PlanWise.Modules.RiskPrediction.Domain.Risks;

// One row per POST /forecasts/run execution, persisted even though only the latest is normally read:
// the thesis needs predictions kept alongside what actually happened later, same rationale as
// CostEstimateRun's run history.
public sealed class RiskAssessmentRun : Entity
{
    private RiskAssessmentRun()
    {
    }

    private RiskAssessmentRun(
        Guid projectId,
        Guid jobId,
        string modelVersion,
        int trainingWindowDays,
        string[] assumptions,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        JobId = jobId;
        ModelVersion = modelVersion;
        TrainingWindowDays = trainingWindowDays;
        Assumptions = assumptions;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ProjectId { get; private set; }
    public Guid JobId { get; private set; }
    public string ModelVersion { get; private set; }
    public int TrainingWindowDays { get; private set; }
    public string[] Assumptions { get; private set; } = [];
    public DateTime CreatedAtUtc { get; private set; }

    public static RiskAssessmentRun Create(
        Guid projectId,
        Guid jobId,
        string modelVersion,
        int trainingWindowDays,
        string[] assumptions,
        DateTime createdAtUtc) =>
        new(projectId, jobId, modelVersion, trainingWindowDays, assumptions, createdAtUtc);
}

using PlanWise.Common.Domain;

namespace PlanWise.Modules.RiskPrediction.Domain.Risks;

// The per-task feature-contribution breakdown is stored as one JSON blob (FeatureContributionsJson)
// rather than child rows, same reasoning as CostEstimateRun.ResultJson: it's read back and
// reserialized for the explanation drawer, never queried or mutated field-by-field.
public sealed class TaskRiskAssessment : Entity
{
    private TaskRiskAssessment()
    {
    }

    private TaskRiskAssessment(
        Guid runId,
        Guid projectId,
        Guid taskId,
        string taskKey,
        decimal probabilityOfSlip,
        int dayImpact,
        string reason,
        string featureContributionsJson,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        RunId = runId;
        ProjectId = projectId;
        TaskId = taskId;
        TaskKey = taskKey;
        ProbabilityOfSlip = probabilityOfSlip;
        DayImpact = dayImpact;
        Reason = reason;
        FeatureContributionsJson = featureContributionsJson;
        CreatedAtUtc = createdAtUtc;
        Dismissed = false;
    }

    public Guid RunId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public string TaskKey { get; private set; }
    public decimal ProbabilityOfSlip { get; private set; }
    public int DayImpact { get; private set; }
    public string Reason { get; private set; }
    public string FeatureContributionsJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public bool Dismissed { get; private set; }
    public DateTime? DismissedAtUtc { get; private set; }
    public string? DismissedReason { get; private set; }

    public static TaskRiskAssessment Create(
        Guid runId,
        Guid projectId,
        Guid taskId,
        string taskKey,
        decimal probabilityOfSlip,
        int dayImpact,
        string reason,
        string featureContributionsJson,
        DateTime createdAtUtc) =>
        new(runId, projectId, taskId, taskKey, probabilityOfSlip, dayImpact, reason, featureContributionsJson, createdAtUtc);

    // Dismissing acknowledges the flag so it stops surfacing in GET /projects/{id}/risks; the next
    // re-run creates a fresh (non-dismissed) assessment row rather than un-dismissing this one, so a
    // still-risky task can be re-flagged on the next run.
    public void Dismiss(string? reason, DateTime nowUtc)
    {
        Dismissed = true;
        DismissedAtUtc = nowUtc;
        DismissedReason = reason;
    }
}

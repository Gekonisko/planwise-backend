using PlanWise.Common.Domain;

namespace PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

public sealed class PriorityItem : Entity
{
    private PriorityItem()
    {
    }

    private PriorityItem(
        Guid runId,
        Guid taskId,
        string taskKey,
        int currentPosition,
        int proposedPosition,
        decimal valueScore,
        decimal dependencyScore,
        decimal complexityScore,
        decimal riskScore,
        string reason)
    {
        Id = Guid.NewGuid();
        RunId = runId;
        TaskId = taskId;
        TaskKey = taskKey;
        CurrentPosition = currentPosition;
        ProposedPosition = proposedPosition;
        ValueScore = valueScore;
        DependencyScore = dependencyScore;
        ComplexityScore = complexityScore;
        RiskScore = riskScore;
        Reason = reason;
    }

    public Guid RunId { get; private set; }
    public Guid TaskId { get; private set; }
    public string TaskKey { get; private set; }
    public int CurrentPosition { get; private set; }
    public int ProposedPosition { get; private set; }
    public decimal ValueScore { get; private set; }
    public decimal DependencyScore { get; private set; }
    public decimal ComplexityScore { get; private set; }
    public decimal RiskScore { get; private set; }
    public string Reason { get; private set; }

    public static PriorityItem Create(
        Guid runId,
        Guid taskId,
        string taskKey,
        int currentPosition,
        int proposedPosition,
        decimal valueScore,
        decimal dependencyScore,
        decimal complexityScore,
        decimal riskScore,
        string reason) =>
        new(runId, taskId, taskKey, currentPosition, proposedPosition, valueScore, dependencyScore, complexityScore, riskScore, reason);
}

using PlanWise.Common.Domain;
using PlanWise.Modules.BacklogPrioritisation.Domain;

namespace PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

public sealed class PriorityRun : Entity
{
    private readonly List<PriorityItem> items = [];

    private PriorityRun()
    {
    }

    private PriorityRun(Guid projectId, Guid jobId, string modelVersion, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        JobId = jobId;
        ModelVersion = modelVersion;
        CreatedAtUtc = createdAtUtc;
        Status = PriorityRunStatus.Pending;
    }

    public Guid ProjectId { get; private set; }
    public Guid JobId { get; private set; }
    public string ModelVersion { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public PriorityRunStatus Status { get; private set; }
    public string? DismissedReason { get; private set; }
    public DateTime? DismissedAtUtc { get; private set; }
    public DateTime? AppliedAtUtc { get; private set; }
    public IReadOnlyCollection<PriorityItem> Items => items;

    public static PriorityRun Create(Guid projectId, Guid jobId, string modelVersion, DateTime createdAtUtc) =>
        new(projectId, jobId, modelVersion, createdAtUtc);

    public PriorityItem AddItem(
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
        var item = PriorityItem.Create(Id, taskId, taskKey, currentPosition, proposedPosition, valueScore, dependencyScore, complexityScore, riskScore, reason);
        items.Add(item);
        return item;
    }

    public Result Apply(DateTime nowUtc)
    {
        if (Status != PriorityRunStatus.Pending)
        {
            return Result.Failure(PriorityErrors.InvalidStateTransition(Id));
        }

        Status = PriorityRunStatus.Applied;
        AppliedAtUtc = nowUtc;
        return Result.Success();
    }

    public Result Dismiss(string? reason, DateTime nowUtc)
    {
        if (Status != PriorityRunStatus.Pending)
        {
            return Result.Failure(PriorityErrors.InvalidStateTransition(Id));
        }

        Status = PriorityRunStatus.Dismissed;
        DismissedReason = reason;
        DismissedAtUtc = nowUtc;
        return Result.Success();
    }
}

public enum PriorityRunStatus
{
    Pending,
    Applied,
    Dismissed
}

using PlanWise.Common.Domain;

namespace PlanWise.Modules.RiskPrediction.Domain.Training;

// One training example: the raw feature vector for a task at the moment a forecast was run, the
// prediction made about it, and — once it is known — what actually happened.
//
// This table exists because the rest of the system stores only *current* state. You cannot ask
// after the fact how many open blockers a task had three weeks ago, so a training set can never be
// reconstructed backwards; it can only be accumulated forwards from the moment capture starts.
// TaskRiskAssessment.FeatureContributionsJson is not a substitute: it records only the scorecard
// factors that fired, as derived weights, so a task that was not overdue leaves no trace of how
// many days it actually had left.
//
// Features are stored as separate typed columns rather than a JSON blob (unlike the assessment's
// contributions) precisely because this table is meant to be queried, aggregated and exported for
// offline training, not read back whole and reserialized.
//
// A task is snapshotted once per forecast run, so the same task yields several examples at
// different points in its life, all eventually sharing one outcome. That is intended — each row is
// a genuine "given what we knew that day, what happened" pair — but it means the rows are
// correlated, and any train/test split must group by TaskId so the same task cannot appear on both
// sides.
public sealed class TaskFeatureSnapshot : Entity
{
    private TaskFeatureSnapshot()
    {
    }

    private TaskFeatureSnapshot(
        Guid runId,
        Guid projectId,
        Guid taskId,
        string taskKey,
        DateTime capturedAtUtc,
        DateOnly capturedOn,
        TaskFeatureVector features,
        decimal predictedProbabilityOfSlip,
        int predictedDayImpact,
        string modelVersion,
        TaskOutcomeStatus initialOutcome)
    {
        Id = Guid.NewGuid();
        RunId = runId;
        ProjectId = projectId;
        TaskId = taskId;
        TaskKey = taskKey;
        CapturedAtUtc = capturedAtUtc;
        CapturedOn = capturedOn;

        Status = features.Status;
        Priority = features.Priority;
        Points = features.Points;
        BusinessValue = features.BusinessValue;
        HasDueDate = features.HasDueDate;
        DaysUntilDue = features.DaysUntilDue;
        DueDate = features.DueDate;
        OpenPredecessorCount = features.OpenPredecessorCount;
        TotalPredecessorCount = features.TotalPredecessorCount;
        BlocksCount = features.BlocksCount;
        SubtaskTotal = features.SubtaskTotal;
        SubtaskDone = features.SubtaskDone;
        IsAssigned = features.IsAssigned;
        AssigneeOpenTaskCount = features.AssigneeOpenTaskCount;
        AssigneeOpenPoints = features.AssigneeOpenPoints;
        IsInSprint = features.IsInSprint;
        DaysLeftInSprint = features.DaysLeftInSprint;
        ProjectOpenTaskCount = features.ProjectOpenTaskCount;

        PredictedProbabilityOfSlip = predictedProbabilityOfSlip;
        PredictedDayImpact = predictedDayImpact;
        ModelVersion = modelVersion;

        OutcomeStatus = initialOutcome;
    }

    public Guid RunId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public string TaskKey { get; private set; }
    public DateTime CapturedAtUtc { get; private set; }

    /// <summary>The local date the snapshot describes — the reference point every day-count is relative to.</summary>
    public DateOnly CapturedOn { get; private set; }

    // ---- features, as they were at CapturedOn ----
    public string Status { get; private set; }
    public string Priority { get; private set; }
    public int? Points { get; private set; }
    public int? BusinessValue { get; private set; }
    public bool HasDueDate { get; private set; }

    /// <summary>Negative when the task was already past its due date when the snapshot was taken.</summary>
    public int? DaysUntilDue { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public int OpenPredecessorCount { get; private set; }
    public int TotalPredecessorCount { get; private set; }
    public int BlocksCount { get; private set; }
    public int SubtaskTotal { get; private set; }
    public int SubtaskDone { get; private set; }
    public bool IsAssigned { get; private set; }
    public int AssigneeOpenTaskCount { get; private set; }
    public int AssigneeOpenPoints { get; private set; }
    public bool IsInSprint { get; private set; }
    public int? DaysLeftInSprint { get; private set; }
    public int ProjectOpenTaskCount { get; private set; }

    // ---- what the model said at the time ----
    public decimal PredictedProbabilityOfSlip { get; private set; }
    public int PredictedDayImpact { get; private set; }
    public string ModelVersion { get; private set; }

    // ---- what actually happened, filled in later ----
    public TaskOutcomeStatus OutcomeStatus { get; private set; }

    /// <summary>Signed: negative means the task finished early. Null until the outcome is known.</summary>
    public int? OutcomeDaysLate { get; private set; }

    /// <summary>
    /// True when Late was inferred from a task still sitting open past its due date rather than from
    /// an actual completion. OutcomeDaysLate is then a lower bound, not a final value — right-censored
    /// data that a naive regression on day-impact would quietly treat as ground truth.
    /// </summary>
    public bool OutcomeIsCensored { get; private set; }

    public DateTime? OutcomeResolvedAtUtc { get; private set; }
    public DateTime? TaskCompletedAtUtc { get; private set; }

    public static TaskFeatureSnapshot Capture(
        Guid runId,
        Guid projectId,
        Guid taskId,
        string taskKey,
        DateTime capturedAtUtc,
        DateOnly capturedOn,
        TaskFeatureVector features,
        decimal predictedProbabilityOfSlip,
        int predictedDayImpact,
        string modelVersion) =>
        new(runId, projectId, taskId, taskKey, capturedAtUtc, capturedOn, features,
            predictedProbabilityOfSlip, predictedDayImpact, modelVersion,
            // A task with no due date has nothing to slip against, so it is born unlabelable rather
            // than sitting Pending forever waiting for an outcome that can never arrive.
            features.HasDueDate ? TaskOutcomeStatus.Pending : TaskOutcomeStatus.NoDueDate);

    /// <summary>The task finished. Days late is signed, so an early finish records as negative.</summary>
    public void ResolveCompleted(int daysLate, DateTime completedAtUtc, DateTime resolvedAtUtc)
    {
        OutcomeStatus = daysLate > 0 ? TaskOutcomeStatus.Late : TaskOutcomeStatus.OnTime;
        OutcomeDaysLate = daysLate;
        OutcomeIsCensored = false;
        TaskCompletedAtUtc = completedAtUtc;
        OutcomeResolvedAtUtc = resolvedAtUtc;
    }

    /// <summary>
    /// The task is still open and already past due. It has definitely slipped, but by at least this
    /// much rather than exactly this much — so it stays censored and keeps being re-resolved on later
    /// runs until the task actually completes.
    /// </summary>
    public void ResolveOpenPastDue(int daysLateSoFar, DateTime resolvedAtUtc)
    {
        OutcomeStatus = TaskOutcomeStatus.Late;
        OutcomeDaysLate = daysLateSoFar;
        OutcomeIsCensored = true;
        OutcomeResolvedAtUtc = resolvedAtUtc;
    }

    /// <summary>Whether a later run should look at this row again.</summary>
    public bool NeedsResolution => OutcomeStatus == TaskOutcomeStatus.Pending || OutcomeIsCensored;
}

public enum TaskOutcomeStatus
{
    /// <summary>Not yet known — the task is still open and not yet past due.</summary>
    Pending,
    OnTime,
    Late,

    /// <summary>No due date, so slip can never be determined for this row.</summary>
    NoDueDate
}

/// <summary>The raw feature vector, kept separate from the entity so extraction stays pure and testable.</summary>
public sealed record TaskFeatureVector(
    string Status,
    string Priority,
    int? Points,
    int? BusinessValue,
    bool HasDueDate,
    int? DaysUntilDue,
    DateOnly? DueDate,
    int OpenPredecessorCount,
    int TotalPredecessorCount,
    int BlocksCount,
    int SubtaskTotal,
    int SubtaskDone,
    bool IsAssigned,
    int AssigneeOpenTaskCount,
    int AssigneeOpenPoints,
    bool IsInSprint,
    int? DaysLeftInSprint,
    int ProjectOpenTaskCount);

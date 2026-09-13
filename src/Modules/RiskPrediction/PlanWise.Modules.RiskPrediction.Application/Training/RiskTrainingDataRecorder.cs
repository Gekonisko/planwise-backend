using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.RiskPrediction.Application.Abstractions;
using PlanWise.Modules.RiskPrediction.Domain.Training;

namespace PlanWise.Modules.RiskPrediction.Application.Training;

// Accumulates the labelled dataset a trained risk model will eventually need. Two jobs, both run on
// every forecast: write this run's feature vectors, and label every row whose outcome is now known.
//
// Labelling is pull-based — resolved from current task state whenever a forecast runs — rather than
// event-driven off task completion. Two reasons. A completion event would never fire for the case
// that matters most, a task quietly sitting open long past its due date; and CompletedAtUtc is
// persisted, so resolving late loses no accuracy, it only delays when the row gets filled in.
//
// This deliberately writes data no code reads yet. Until capture is running there is nothing to
// train on later, and unlike most data it cannot be backfilled: the system stores current state
// only, so a feature vector not written on the day is gone.
public sealed class RiskTrainingDataRecorder(ITaskFeatureSnapshotRepository snapshotRepository)
{
    private const string DoneStatus = "Done";

    /// <summary>
    /// Captures this run's feature vectors and resolves every outcome that is now knowable — both on
    /// earlier rows and on the ones just captured.
    /// </summary>
    public async Task RecordAsync(
        Guid runId,
        RiskPredictionInput input,
        RiskPredictionResult prediction,
        string modelVersion,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        List<TaskFeatureSnapshot> captured = Capture(runId, input, prediction, modelVersion, nowUtc);

        // The repository query runs against the database, which cannot yet see rows added in this
        // same transaction — so the freshly captured ones are folded in explicitly. Without this a
        // task captured while already overdue would sit Pending until the *next* forecast run.
        IReadOnlyList<TaskFeatureSnapshot> stored =
            await snapshotRepository.GetUnresolvedAsync(input.ProjectId, cancellationToken);

        Resolve(stored.Concat(captured), input.Tasks, input.Today, nowUtc);
    }

    /// <summary>Writes one row per scored task: the features it had today, and what was predicted.</summary>
    private List<TaskFeatureSnapshot> Capture(
        Guid runId,
        RiskPredictionInput input,
        RiskPredictionResult prediction,
        string modelVersion,
        DateTime capturedAtUtc)
    {
        var tasksById = input.Tasks.ToDictionary(task => task.TaskId);
        var sprintsById = input.Sprints.ToDictionary(sprint => sprint.SprintId);
        ProjectLoad load = TaskFeatureExtractor.ComputeLoad(input.Tasks);

        var snapshots = new List<TaskFeatureSnapshot>(prediction.TaskRisks.Count);
        foreach (TaskRiskPrediction risk in prediction.TaskRisks)
        {
            // A model may score something that wasn't in the input; there is no feature vector to
            // record for it, so it contributes no training example.
            if (!tasksById.TryGetValue(risk.TaskId, out TaskInsightSummary? task))
            {
                continue;
            }

            TaskFeatureVector features = TaskFeatureExtractor.Extract(task, tasksById, sprintsById, load, input.Today);

            snapshots.Add(TaskFeatureSnapshot.Capture(
                runId,
                input.ProjectId,
                task.TaskId,
                task.Key,
                capturedAtUtc,
                input.Today,
                features,
                risk.ProbabilityOfSlip,
                risk.DayImpact,
                modelVersion));
        }

        snapshotRepository.AddRange(snapshots);
        return snapshots;
    }

    /// <summary>
    /// Fills in outcomes wherever they are now knowable. Censored rows are revisited too: a task that
    /// was open-and-overdue last week may have finished since, which turns a lower bound into a real
    /// number.
    /// </summary>
    private static void Resolve(
        IEnumerable<TaskFeatureSnapshot> snapshots,
        IReadOnlyList<TaskInsightSummary> currentTasks,
        DateOnly today,
        DateTime resolvedAtUtc)
    {
        var tasksById = currentTasks.ToDictionary(task => task.TaskId);

        foreach (TaskFeatureSnapshot snapshot in snapshots.Where(snapshot => snapshot.NeedsResolution))
        {
            // Snapshots are only ever taken for tasks that had a due date, but a due date can be
            // cleared afterwards, and a task can be deleted outright. Neither can be labelled.
            if (snapshot.DueDate is not DateOnly dueDate ||
                !tasksById.TryGetValue(snapshot.TaskId, out TaskInsightSummary? task))
            {
                continue;
            }

            if (task.Status == DoneStatus && task.CompletedAtUtc is DateTime completedAtUtc)
            {
                var completedOn = DateOnly.FromDateTime(completedAtUtc);
                snapshot.ResolveCompleted(completedOn.DayNumber - dueDate.DayNumber, completedAtUtc, resolvedAtUtc);
                continue;
            }

            // Still open. Only past-due tells us anything yet; anything else stays Pending.
            int daysLateSoFar = today.DayNumber - dueDate.DayNumber;
            if (daysLateSoFar > 0)
            {
                snapshot.ResolveOpenPastDue(daysLateSoFar, resolvedAtUtc);
            }
        }
    }
}

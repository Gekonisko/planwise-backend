using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.RiskPrediction.Domain.Training;

namespace PlanWise.Modules.RiskPrediction.Application.Training;

// Turns the state a forecast run saw into the raw feature vector a model could be trained on.
//
// Deliberately raw, not derived: no weighting, no bucketing, no scoring. The weighted scorecard's
// judgements (what counts as "due soon", how much a blocker is worth) are exactly the assumptions a
// trained model is supposed to learn for itself, so baking them in here would leak the baseline's
// opinions into its own replacement's training data.
internal static class TaskFeatureExtractor
{
    public static TaskFeatureVector Extract(
        TaskInsightSummary task,
        IReadOnlyDictionary<Guid, TaskInsightSummary> allTasksById,
        IReadOnlyDictionary<Guid, SprintInsightSummary> sprintsById,
        ProjectLoad projectLoad,
        DateOnly today)
    {
        int openPredecessors = task.PredecessorTaskIds.Count(id =>
            allTasksById.TryGetValue(id, out TaskInsightSummary? predecessor) && predecessor.Status != DoneStatus);

        int? daysUntilDue = task.DueDate is DateOnly dueDate ? dueDate.DayNumber - today.DayNumber : null;

        int? daysLeftInSprint = task.SprintId is Guid sprintId && sprintsById.TryGetValue(sprintId, out SprintInsightSummary? sprint)
            ? sprint.EndDate.DayNumber - today.DayNumber
            : null;

        AssigneeLoad assigneeLoad = task.AssigneeId is Guid assigneeId
            ? projectLoad.ByAssignee.GetValueOrDefault(assigneeId, AssigneeLoad.None)
            : AssigneeLoad.None;

        return new TaskFeatureVector(
            task.Status,
            task.Priority,
            task.Points,
            task.BusinessValue,
            task.DueDate is not null,
            daysUntilDue,
            task.DueDate,
            openPredecessors,
            task.PredecessorTaskIds.Count,
            task.BlocksCount,
            task.SubtaskTotal,
            task.SubtaskDone,
            task.AssigneeId is not null,
            assigneeLoad.OpenTaskCount,
            assigneeLoad.OpenPoints,
            task.SprintId is not null,
            daysLeftInSprint,
            projectLoad.OpenTaskCount);
    }

    /// <summary>
    /// How loaded the project and each person on it were at snapshot time. Computed once per run
    /// rather than per task — how much a person already has on their plate plausibly predicts slip,
    /// but recomputing it inside the per-task loop would be quadratic.
    /// </summary>
    public static ProjectLoad ComputeLoad(IReadOnlyList<TaskInsightSummary> allTasks)
    {
        var openTasks = allTasks.Where(task => task.Status != DoneStatus).ToList();

        var byAssignee = openTasks
            .Where(task => task.AssigneeId is not null)
            .GroupBy(task => task.AssigneeId!.Value)
            .ToDictionary(
                group => group.Key,
                group => new AssigneeLoad(group.Count(), group.Sum(task => task.Points ?? 0)));

        return new ProjectLoad(openTasks.Count, byAssignee);
    }

    private const string DoneStatus = "Done";
}

public sealed record AssigneeLoad(int OpenTaskCount, int OpenPoints)
{
    public static readonly AssigneeLoad None = new(0, 0);
}

public sealed record ProjectLoad(int OpenTaskCount, IReadOnlyDictionary<Guid, AssigneeLoad> ByAssignee);

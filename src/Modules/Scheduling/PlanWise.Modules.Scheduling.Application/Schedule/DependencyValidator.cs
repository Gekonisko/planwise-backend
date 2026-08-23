using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Modules.Scheduling.Application.Schedule;

// Enforces the primary Gantt drag-and-drop rule: a task cannot start before its predecessors finish.
// Used by both the single-item PATCH (which persists) and the batch validate endpoint (which never
// persists), so the two stay consistent.
internal static class DependencyValidator
{
    public static string? FindViolation(
        Guid taskId,
        DateOnly newStartDate,
        IReadOnlyDictionary<Guid, ScheduleTaskSummary> tasksById,
        IReadOnlyDictionary<Guid, DateOnly> effectiveEndDates)
    {
        if (!tasksById.TryGetValue(taskId, out ScheduleTaskSummary? task))
        {
            return null;
        }

        foreach (Guid predecessorId in task.PredecessorTaskIds)
        {
            if (effectiveEndDates.TryGetValue(predecessorId, out DateOnly predecessorEnd) && predecessorEnd > newStartDate)
            {
                string predecessorKey = tasksById.TryGetValue(predecessorId, out ScheduleTaskSummary? predecessor)
                    ? predecessor.Key
                    : predecessorId.ToString();

                return $"Task {task.Key} cannot start on {newStartDate:yyyy-MM-dd} before predecessor {predecessorKey} finishes on {predecessorEnd:yyyy-MM-dd}";
            }
        }

        return null;
    }
}

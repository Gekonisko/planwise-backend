using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Modules.Scheduling.Application.Schedule;

// Deterministic critical-path (CPM) calculator: forward pass gives each task the earliest it can
// start/finish given its predecessors, backward pass gives the latest it can start/finish without
// pushing the project end date out; slack is the gap between the two, and slack <= 0 marks the
// critical path. A manually scheduled task (an override) is treated as a hard constraint rather than
// a computed value, since the user placed it there deliberately.
internal static class ScheduleCalculator
{
    public static IReadOnlyDictionary<Guid, ComputedTaskSchedule> Compute(
        IReadOnlyList<ScheduleTaskSummary> tasks,
        IReadOnlyDictionary<Guid, (DateOnly StartDate, DateOnly EndDate)> overrides,
        DateOnly today)
    {
        var byId = tasks.ToDictionary(task => task.TaskId);
        Dictionary<Guid, List<Guid>> successors = BuildSuccessors(tasks, byId);
        List<Guid> order = TopologicalOrder(tasks, successors);

        var earliestStart = new Dictionary<Guid, DateOnly>();
        var earliestFinish = new Dictionary<Guid, DateOnly>();

        foreach (Guid taskId in order)
        {
            ScheduleTaskSummary task = byId[taskId];

            if (overrides.TryGetValue(taskId, out (DateOnly StartDate, DateOnly EndDate) manual))
            {
                earliestStart[taskId] = manual.StartDate;
                earliestFinish[taskId] = manual.EndDate;
                continue;
            }

            DateOnly start = today;
            foreach (Guid predecessorId in task.PredecessorTaskIds)
            {
                if (earliestFinish.TryGetValue(predecessorId, out DateOnly predecessorFinish) && predecessorFinish > start)
                {
                    start = predecessorFinish;
                }
            }

            earliestStart[taskId] = start;
            earliestFinish[taskId] = start.AddDays(DurationDays(task));
        }

        DateOnly projectEnd = earliestFinish.Count > 0 ? earliestFinish.Values.Max() : today;

        var latestStart = new Dictionary<Guid, DateOnly>();

        foreach (Guid taskId in Enumerable.Reverse(order))
        {
            ScheduleTaskSummary task = byId[taskId];
            List<Guid> taskSuccessors = successors[taskId];

            DateOnly finish = taskSuccessors.Count == 0
                ? projectEnd
                : taskSuccessors.Min(successorId => latestStart.TryGetValue(successorId, out DateOnly ls) ? ls : projectEnd);

            int duration = overrides.TryGetValue(taskId, out (DateOnly StartDate, DateOnly EndDate) manual)
                ? Math.Max(1, manual.EndDate.DayNumber - manual.StartDate.DayNumber)
                : DurationDays(task);

            latestStart[taskId] = finish.AddDays(-duration);
        }

        var result = new Dictionary<Guid, ComputedTaskSchedule>();
        foreach (Guid taskId in order)
        {
            int slack = latestStart[taskId].DayNumber - earliestStart[taskId].DayNumber;
            result[taskId] = new ComputedTaskSchedule(earliestStart[taskId], earliestFinish[taskId], slack, slack <= 0);
        }

        return result;
    }

    // 1 story point ~= 1 day; tasks without points default to a single day. There is no separate
    // estimate field on ProjectTask yet, so points double as the duration signal for the Gantt.
    private static int DurationDays(ScheduleTaskSummary task) => Math.Max(1, task.Points ?? 1);

    private static Dictionary<Guid, List<Guid>> BuildSuccessors(
        IReadOnlyList<ScheduleTaskSummary> tasks,
        Dictionary<Guid, ScheduleTaskSummary> byId)
    {
        var successors = tasks.ToDictionary(task => task.TaskId, _ => new List<Guid>());

        foreach (ScheduleTaskSummary task in tasks)
        {
            foreach (Guid predecessorId in task.PredecessorTaskIds)
            {
                if (byId.ContainsKey(predecessorId))
                {
                    successors[predecessorId].Add(task.TaskId);
                }
            }
        }

        return successors;
    }

    // Kahn's algorithm. Task links have no cycle validation when created, so any tasks left over
    // once every resolvable node has been dequeued are part of a cycle; they're appended in their
    // original order so the schedule still renders (with best-effort dates) instead of throwing.
    private static List<Guid> TopologicalOrder(IReadOnlyList<ScheduleTaskSummary> tasks, Dictionary<Guid, List<Guid>> successors)
    {
        var indegree = tasks.ToDictionary(task => task.TaskId, _ => 0);

        foreach (List<Guid> taskSuccessors in successors.Values)
        {
            foreach (Guid successorId in taskSuccessors)
            {
                indegree[successorId]++;
            }
        }

        var queue = new Queue<Guid>(indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var order = new List<Guid>();

        while (queue.Count > 0)
        {
            Guid taskId = queue.Dequeue();
            order.Add(taskId);

            foreach (Guid successorId in successors[taskId])
            {
                if (--indegree[successorId] == 0)
                {
                    queue.Enqueue(successorId);
                }
            }
        }

        if (order.Count < tasks.Count)
        {
            var resolved = new HashSet<Guid>(order);
            order.AddRange(tasks.Select(task => task.TaskId).Where(taskId => !resolved.Contains(taskId)));
        }

        return order;
    }

    public sealed record ComputedTaskSchedule(DateOnly StartDate, DateOnly EndDate, int SlackDays, bool IsCritical);
}

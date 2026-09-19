using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Optimisation;

// The v1 optimiser, unchanged in behaviour and kept runnable: a deterministic greedy load-balancer.
// It walks the unassigned tasks critical-path-first and hands each to whichever member currently has
// the lowest points-per-capacity, preferring one whose skill tags appear in the task's title.
//
// It is worth keeping for the same reason WeightedScorecardRiskModel is: a solver that cannot beat
// the greedy baseline on a real project is not earning its dependency, and that is only a claim you
// can check if the baseline still runs. It is also the fallback when CP-SAT cannot return a solution.
//
// What it cannot do, and why CpSatScheduleOptimiser exists: it commits to each assignment in turn
// without any model of when the work would actually happen, so it cannot see that giving a critical
// task to a busy person pushes the whole project out. Balance is a proxy for the goal, not the goal.
public sealed class GreedyCapacityBalancer : IScheduleOptimisationModel
{
    public const string Name = "GreedyCapacityBalancer v1";

    private const string ObjectiveText =
        "Balance workload for unassigned backlog tasks across project members by remaining capacity, preferring a member whose skill tags match the task's title";

    public Task<ScheduleOptimisationResult> OptimiseAsync(
        ScheduleOptimisationInput input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Optimise(input));

    public static ScheduleOptimisationResult Optimise(ScheduleOptimisationInput input)
    {
        var eligibleMembers = input.Members
            .Where(member => member.UserId is not null && member.Capacity > 0)
            .ToList();

        IReadOnlyDictionary<Guid, ScheduleCalculator.ComputedTaskSchedule> computedSchedule =
            ScheduleCalculator.Compute(input.Tasks, new Dictionary<Guid, (DateOnly StartDate, DateOnly EndDate)>(), input.Today);

        var load = eligibleMembers.ToDictionary(member => member.UserId!.Value, _ => 0);
        foreach (ScheduleTaskSummary task in input.Tasks)
        {
            if (task.AssigneeId is Guid assigneeId && !task.IsDone && load.ContainsKey(assigneeId))
            {
                load[assigneeId] += task.Points ?? 0;
            }
        }

        Dictionary<Guid, int> loadBefore = new(load);

        var unassignedTasks = input.Tasks
            .Where(task => task.AssigneeId is null && !task.IsDone)
            .OrderByDescending(task => computedSchedule.TryGetValue(task.TaskId, out ScheduleCalculator.ComputedTaskSchedule? s) && s.IsCritical)
            .ThenByDescending(task => task.Points ?? 0)
            .ToList();

        var assignments = new List<ProposedTaskAssignment>();
        int skillMatchedAssignments = 0;

        foreach (ScheduleTaskSummary task in unassignedTasks)
        {
            if (eligibleMembers.Count == 0)
            {
                break;
            }

            ProjectMemberSummary chosen = eligibleMembers
                .OrderByDescending(member => SkillMatchCount(member, task))
                .ThenBy(member => load[member.UserId!.Value] / member.Capacity)
                .First();

            if (SkillMatchCount(chosen, task) > 0)
            {
                skillMatchedAssignments++;
            }

            assignments.Add(new ProposedTaskAssignment(
                task.TaskId, task.Key, task.AssigneeId, chosen.UserId!.Value, chosen.Email));
            load[chosen.UserId!.Value] += task.Points ?? 0;
        }

        return new ScheduleOptimisationResult(
            Name,
            ObjectiveText,
            assignments,
            ConstraintsHonoured,
            ConstraintsRelaxed,
            BuildExpectedGain(unassignedTasks.Count, eligibleMembers.Count, loadBefore, load, skillMatchedAssignments));
    }

    // A member's skill tag counts as a match if it appears as a substring of the task's title — the
    // only per-task text available to match against, since no task carries a structured
    // required-skill field. Case-insensitive; an unskilled member (no tags set) never matches.
    public static int SkillMatchCount(ProjectMemberSummary member, ScheduleTaskSummary task) =>
        member.Skills.Count(skill => task.Title.Contains(skill, StringComparison.OrdinalIgnoreCase));

    private static readonly string[] ConstraintsHonoured =
    [
        "Existing assignments on already-assigned tasks were not changed",
        "Completed tasks were not reassigned",
        "Dependency ordering and dates from the current schedule were not altered",
        "A member whose skill tags matched the task's title was preferred over one with no match"
    ];

    private static readonly string[] ConstraintsRelaxed =
    [
        "Assignments are chosen one at a time by current load, with no model of when the work would actually run — so the effect of an assignment on the project's finish date is not considered",
        "Skill matching is a title-substring heuristic, not true competency matching — no task carries a structured required-skill field",
        "Member calendar-specific availability (holidays, leave) not modelled — capacity is treated as a constant figure"
    ];

    private static string BuildExpectedGain(
        int unassignedTaskCount,
        int eligibleMemberCount,
        IReadOnlyDictionary<Guid, int> before,
        IReadOnlyDictionary<Guid, int> after,
        int skillMatchedAssignments)
    {
        if (unassignedTaskCount == 0)
        {
            return "No unassigned tasks to balance — nothing proposed";
        }

        if (eligibleMemberCount == 0)
        {
            return "No members with available capacity — nothing proposed";
        }

        int beforeImbalance = before.Count == 0 ? 0 : before.Values.Max() - before.Values.Min();
        int afterImbalance = after.Count == 0 ? 0 : after.Values.Max() - after.Values.Min();

        return $"Reduces max/min assigned-points imbalance across members from {beforeImbalance} to {afterImbalance}; " +
               $"{skillMatchedAssignments} of {unassignedTaskCount} assignment(s) matched on skill tags";
    }
}

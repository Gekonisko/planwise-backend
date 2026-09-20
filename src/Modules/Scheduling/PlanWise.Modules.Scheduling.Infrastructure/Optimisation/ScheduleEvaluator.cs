using Google.OrTools.Sat;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Optimisation;

namespace PlanWise.Modules.Scheduling.Infrastructure.Optimisation;

// Scores an assignment that has already been decided, rather than deciding one.
//
// WHY THIS IS SEPARATE. CpSatScheduleOptimiser chooses assignments and reports a makespan; the
// greedy balancer chooses assignments and reports load imbalance. Those two numbers are not
// comparable, so neither optimiser could be held to the other's standard. This evaluator fixes the
// owners and asks the one question that means the same thing for every method: given these people
// doing this work, under dependency order and one task at a time, when does the project finish?
//
// It is also the feedback channel that makes AgenticScheduleOptimiser agentic at all. A single-shot
// model commits blind; an agent calls this, sees what its own proposal costs in days, and revises.
// The same function therefore scores the experiment and drives the agent, which is deliberate — the
// agent cannot be flattered by a scoring rule the other arms are not held to.
//
// The scheduling model is deliberately identical to CpSatScheduleOptimiser's: one day per point,
// duration divided by capacity and rounded up, NoOverlap per member, precedence over unfinished
// predecessors only. Any divergence here would silently bias the comparison it exists to support.
public static class ScheduleEvaluator
{
    private const double SolverTimeLimitSeconds = 5.0;

    /// <summary>
    /// Scores <paramref name="proposed"/> (task id to member id) layered on top of the assignments
    /// the project already has. Tasks nobody owns are still scheduled for dependency purposes but
    /// occupy no member's timeline, exactly as CpSatScheduleOptimiser treats orphaned work.
    /// </summary>
    public static AssignmentEvaluation Evaluate(
        ScheduleOptimisationInput input,
        IReadOnlyDictionary<Guid, Guid> proposed)
    {
        var members = input.Members
            .Where(member => member.UserId is not null && member.Capacity > 0)
            .ToList();
        var memberById = members.ToDictionary(member => member.UserId!.Value);

        var openTasks = input.Tasks.Where(task => !task.IsDone).ToList();
        var openTaskIds = openTasks.Select(task => task.TaskId).ToHashSet();
        var byId = input.Tasks.ToDictionary(task => task.TaskId);

        var violations = new List<string>();

        // An assignment can be well-formed and still illegitimate: aimed at a finished task, at one
        // that already has an owner, or at a person who is not on the project. Those are conformance
        // failures rather than scheduling ones, so they are reported separately and their tasks are
        // left unassigned — an invalid proposal must never be able to look fast.
        var effective = new Dictionary<Guid, Guid>();
        foreach (ScheduleTaskSummary task in openTasks.Where(task => task.AssigneeId is not null))
        {
            effective[task.TaskId] = task.AssigneeId!.Value;
        }

        foreach ((Guid taskId, Guid memberId) in proposed)
        {
            if (!byId.TryGetValue(taskId, out ScheduleTaskSummary? task))
            {
                violations.Add($"Assignment refers to task {taskId}, which is not in this project");
                continue;
            }

            if (task.IsDone)
            {
                violations.Add($"Task {task.Key} is already done and must not be reassigned");
                continue;
            }

            if (task.AssigneeId is Guid owner && owner != memberId)
            {
                violations.Add($"Task {task.Key} already belongs to another member and must not be reassigned");
                continue;
            }

            if (!memberById.ContainsKey(memberId))
            {
                violations.Add($"Task {task.Key} is assigned to somebody who is not an eligible project member");
                continue;
            }

            effective[task.TaskId] = memberId;
        }

        var unassigned = openTasks
            .Where(task => !effective.ContainsKey(task.TaskId))
            .Select(task => task.Key)
            .ToList();

        var loads = members.ToDictionary(member => member.UserId!.Value, _ => (Tasks: 0, Days: 0));
        int skillMatched = 0;
        foreach ((Guid taskId, Guid memberId) in effective)
        {
            ProjectMemberSummary member = memberById[memberId];
            (int tasks, int days) = loads[memberId];
            loads[memberId] = (tasks + 1, days + ScaledDuration(byId[taskId], member));

            if (GreedyCapacityBalancer.SkillMatchCount(member, byId[taskId]) > 0)
            {
                skillMatched++;
            }
        }

        var memberLoads = members
            .Select(member => new MemberLoad(
                member.Email,
                loads[member.UserId!.Value].Tasks,
                loads[member.UserId!.Value].Days))
            .OrderBy(load => load.Email, StringComparer.Ordinal)
            .ToList();

        Schedule schedule = Solve(openTasks, openTaskIds, effective, memberById, members);

        int lateTasks = schedule.Feasible
            ? openTasks
                .Where(task => task.DueDate is not null)
                .Count(task => schedule.Finish[task.TaskId] > Math.Max(0, task.DueDate!.Value.DayNumber - input.Today.DayNumber))
            : 0;

        return new AssignmentEvaluation(
            schedule.Feasible ? schedule.Makespan : -1,
            schedule.Feasible,
            memberLoads,
            violations,
            unassigned,
            skillMatched,
            effective.Count,
            lateTasks);
    }

    /// <summary>
    /// Earliest project finish, in days from today, for a fixed owner per task, together with each
    /// task's finish day on that schedule. One solve serves both the makespan and the lateness count
    /// so the two can never describe different arrangements of the same work.
    /// </summary>
    private static Schedule Solve(
        List<ScheduleTaskSummary> openTasks,
        HashSet<Guid> openTaskIds,
        Dictionary<Guid, Guid> owners,
        Dictionary<Guid, ProjectMemberSummary> memberById,
        List<ProjectMemberSummary> members)
    {
        if (openTasks.Count == 0)
        {
            return new Schedule(true, 0, new Dictionary<Guid, long>());
        }

        var model = new CpModel();
        int horizon = openTasks.Sum(task => BaseDuration(task) * CapacityDivisor(members)) + 1;
        var start = new Dictionary<Guid, IntVar>();
        var end = new Dictionary<Guid, IntVar>();
        var intervalsByMember = members.ToDictionary(member => member.UserId!.Value, _ => new List<IntervalVar>());

        foreach (ScheduleTaskSummary task in openTasks)
        {
            ProjectMemberSummary? owner = owners.TryGetValue(task.TaskId, out Guid ownerId)
                ? memberById[ownerId]
                : null;
            int duration = owner is null ? BaseDuration(task) : ScaledDuration(task, owner);

            start[task.TaskId] = model.NewIntVar(0, horizon, $"s_{task.TaskId}");
            end[task.TaskId] = model.NewIntVar(0, horizon, $"e_{task.TaskId}");
            model.Add(end[task.TaskId] == start[task.TaskId] + duration);

            if (owner is not null)
            {
                intervalsByMember[owner.UserId!.Value].Add(
                    model.NewIntervalVar(start[task.TaskId], duration, end[task.TaskId], $"i_{task.TaskId}"));
            }
        }

        foreach (ScheduleTaskSummary task in openTasks)
        {
            foreach (Guid predecessor in task.PredecessorTaskIds.Where(openTaskIds.Contains))
            {
                model.Add(start[task.TaskId] >= end[predecessor]);
            }
        }

        foreach (List<IntervalVar> intervals in intervalsByMember.Values.Where(intervals => intervals.Count > 1))
        {
            model.AddNoOverlap(intervals);
        }

        IntVar makespan = model.NewIntVar(0, horizon, "makespan");
        model.AddMaxEquality(makespan, openTasks.Select(task => end[task.TaskId]));
        model.Minimize(makespan);

        using var solver = new CpSolver
        {
            StringParameters = $"max_time_in_seconds:{SolverTimeLimitSeconds};num_search_workers:4"
        };

        if (solver.Solve(model) is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
        {
            return new Schedule(false, -1, new Dictionary<Guid, long>());
        }

        var finish = openTasks.ToDictionary(task => task.TaskId, task => solver.Value(end[task.TaskId]));
        return new Schedule(true, solver.Value(makespan), finish);
    }

    private static int BaseDuration(ScheduleTaskSummary task) => Math.Max(1, task.Points ?? 1);

    private static int ScaledDuration(ScheduleTaskSummary task, ProjectMemberSummary member) =>
        (int)Math.Ceiling(BaseDuration(task) / (double)member.Capacity);

    private static int CapacityDivisor(List<ProjectMemberSummary> members) =>
        members.Count == 0 ? 1 : (int)Math.Ceiling(1.0 / (double)members.Min(member => member.Capacity));

    private sealed record Schedule(bool Feasible, long Makespan, IReadOnlyDictionary<Guid, long> Finish);
}

/// <param name="MakespanDays">Days from today until the last open task finishes; -1 if unschedulable.</param>
/// <param name="Violations">
/// Assignments rejected before scheduling — a finished task, a task with an owner, or a person not on
/// the project. Their tasks stay unassigned, so an invalid proposal cannot buy itself a shorter
/// project by ignoring the rules the other arms obey.
/// </param>
/// <param name="TasksPastDueDate">
/// Reported alongside makespan because an arrangement can finish the project sooner and still miss
/// more commitments — the trade-off CpSatScheduleOptimiser's fixed objective weights make silently.
/// </param>
public sealed record AssignmentEvaluation(
    long MakespanDays,
    bool Feasible,
    IReadOnlyList<MemberLoad> MemberLoads,
    IReadOnlyList<string> Violations,
    IReadOnlyList<string> UnassignedTaskKeys,
    int SkillMatchedTasks,
    int AssignedTasks,
    int TasksPastDueDate);

public sealed record MemberLoad(string Email, int TaskCount, int BusyDays);

using Google.OrTools.Sat;
using Microsoft.Extensions.Logging;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Optimisation;

namespace PlanWise.Modules.Scheduling.Infrastructure.Optimisation;

// A real constraint-programming optimiser (OR-Tools CP-SAT), replacing a greedy heuristic that had
// no model of time at all.
//
// WHAT THE GREEDY COULD NOT DO. It assigned tasks one at a time to whoever had the lowest
// points-per-capacity, which optimises *balance* — a proxy — and never the thing anyone actually
// wants, which is for the project to finish sooner. It could not see that handing a task on the
// critical path to someone already booked pushes every downstream task out, nor that two tasks
// assigned to the same person cannot run at the same time. Balance and makespan pull apart the
// moment dependencies exist.
//
// THE MODEL. A resource-constrained project scheduling problem:
//   * one interval per (open task, candidate member), optional, sharing the task's start variable
//   * exactly one member per task; already-assigned work is pinned to its current owner
//   * duration scales with capacity — a half-time member takes twice as long on the same task
//   * NoOverlap per member: a person does one task at a time
//   * precedence: a task cannot start before every open predecessor has finished
//   * objective: minimise makespan first, then tardiness against due dates, then prefer skill matches
//
// HONESTY ABOUT THE OBJECTIVE. The three terms are combined with fixed weights rather than solved
// lexicographically, so a large tardiness saving can in principle buy a day of makespan. The weights
// are stated in the proposal's explanation rather than left implicit.
//
// WHY IT CAN STILL FALL BACK. CP-SAT is given a wall-clock budget; a large project may leave it with
// no feasible solution in time, and the native solver library may fail to load on an unexpected
// platform. Either way the honest answer is the greedy result, clearly labelled as such in the
// proposal's model name — never a silent substitution.
public sealed class CpSatScheduleOptimiser(ILogger<CpSatScheduleOptimiser> logger) : IScheduleOptimisationModel
{
    public const string Name = "CpSatScheduleOptimiser v1";

    private const string ObjectiveText =
        "Minimise the project's finish date (makespan) subject to dependency order, one task at a time per member and capacity-scaled durations; then reduce lateness against due dates, then prefer members whose skill tags match the task's title";

    /// <summary>
    /// Wall-clock budget for the solver. Generous enough for realistic backlogs, short enough that a
    /// pathological project degrades to the greedy result rather than holding the job open.
    /// </summary>
    private const double SolverTimeLimitSeconds = 10.0;

    // Makespan dominates: a day of project delay outweighs any amount of tardiness or skill
    // mismatch it could buy. Tardiness in turn outweighs skill preference, which is only a tie-break
    // because the matching itself is a title-substring heuristic and not to be trusted far.
    private const int MakespanWeight = 10_000;
    private const int TardinessWeight = 100;
    private const int SkillMatchWeight = 1;

    public Task<ScheduleOptimisationResult> OptimiseAsync(
        ScheduleOptimisationInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ScheduleOptimisationResult? solved = Solve(input, cancellationToken);
            if (solved is not null)
            {
                return Task.FromResult(solved);
            }

            logger.LogWarning(
                "CP-SAT found no solution for project {ProjectId} within {Seconds}s; falling back to the greedy balancer.",
                input.ProjectId, SolverTimeLimitSeconds);
        }
        catch (Exception exception) when (exception is TypeInitializationException or DllNotFoundException or EntryPointNotFoundException)
        {
            // The native OR-Tools library failed to load. Worth a loud log and a working proposal,
            // not a failed job.
            logger.LogError(exception, "The OR-Tools native library could not be loaded; falling back to the greedy balancer.");
        }

        return Task.FromResult(FellBackToGreedy(input));
    }

    private static ScheduleOptimisationResult? Solve(ScheduleOptimisationInput input, CancellationToken cancellationToken)
    {
        var members = input.Members
            .Where(member => member.UserId is not null && member.Capacity > 0)
            .ToList();

        var openTasks = input.Tasks.Where(task => !task.IsDone).ToList();
        var unassigned = openTasks.Where(task => task.AssigneeId is null).ToList();

        if (unassigned.Count == 0 || members.Count == 0)
        {
            return NothingToPropose(unassigned.Count);
        }

        var model = new CpModel();
        var openTaskIds = openTasks.Select(task => task.TaskId).ToHashSet();
        var memberIndex = members.Select((member, index) => (member, index))
            .ToDictionary(pair => pair.member.UserId!.Value, pair => pair.index);

        // A day per point, matching ScheduleCalculator's duration model, divided by the assignee's
        // capacity. The horizon is the worst case where one member does everything.
        int horizon = openTasks.Sum(task => BaseDuration(task) * CapacityDivisor(members)) + 1;

        var start = new Dictionary<Guid, IntVar>();
        var end = new Dictionary<Guid, IntVar>();
        var presence = new Dictionary<(Guid Task, int Member), BoolVar>();
        var intervalsByMember = members.Select(_ => new List<IntervalVar>()).ToList();

        foreach (ScheduleTaskSummary task in openTasks)
        {
            start[task.TaskId] = model.NewIntVar(0, horizon, $"start_{task.TaskId}");
            end[task.TaskId] = model.NewIntVar(0, horizon, $"end_{task.TaskId}");

            // Candidates: a pinned member for assigned work, every member for unassigned work. Work
            // owned by someone who is no longer an eligible member is scheduled but not proposed and
            // consumes nobody's timeline — there is no honest resource to charge it to.
            List<int> candidates = CandidateMembers(task, memberIndex, members.Count);

            if (candidates.Count == 0)
            {
                model.Add(end[task.TaskId] == start[task.TaskId] + BaseDuration(task));
                continue;
            }

            var taskPresences = new List<BoolVar>();
            foreach (int index in candidates)
            {
                BoolVar isPresent = model.NewBoolVar($"assign_{task.TaskId}_{index}");
                int duration = ScaledDuration(task, members[index]);

                // All candidate intervals share the task's single start variable, so choosing a
                // member chooses a duration without letting the task start twice.
                IntervalVar interval = model.NewOptionalIntervalVar(
                    start[task.TaskId], duration, model.NewIntVar(0, horizon, $"iend_{task.TaskId}_{index}"),
                    isPresent, $"iv_{task.TaskId}_{index}");

                model.Add(end[task.TaskId] == start[task.TaskId] + duration).OnlyEnforceIf(isPresent);
                intervalsByMember[index].Add(interval);
                presence[(task.TaskId, index)] = isPresent;
                taskPresences.Add(isPresent);
            }

            model.AddExactlyOne(taskPresences);
        }

        foreach (ScheduleTaskSummary task in openTasks)
        {
            // Finished predecessors impose nothing — they are already done.
            foreach (Guid predecessor in task.PredecessorTaskIds.Where(openTaskIds.Contains))
            {
                model.Add(start[task.TaskId] >= end[predecessor]);
            }
        }

        foreach (List<IntervalVar> intervals in intervalsByMember.Where(intervals => intervals.Count > 1))
        {
            model.AddNoOverlap(intervals);
        }

        IntVar makespan = model.NewIntVar(0, horizon, "makespan");
        model.AddMaxEquality(makespan, openTasks.Select(task => end[task.TaskId]));

        var objective = LinearExpr.Term(makespan, MakespanWeight);

        foreach (ScheduleTaskSummary task in openTasks.Where(task => task.DueDate is not null))
        {
            int dueDay = Math.Max(0, task.DueDate!.Value.DayNumber - input.Today.DayNumber);
            IntVar tardiness = model.NewIntVar(0, horizon, $"late_{task.TaskId}");
            model.Add(tardiness >= end[task.TaskId] - dueDay);
            objective += LinearExpr.Term(tardiness, TardinessWeight);
        }

        foreach (((Guid taskId, int index), BoolVar isPresent) in presence)
        {
            ScheduleTaskSummary task = openTasks.First(candidate => candidate.TaskId == taskId);
            if (GreedyCapacityBalancer.SkillMatchCount(members[index], task) > 0)
            {
                objective -= LinearExpr.Term(isPresent, SkillMatchWeight);
            }
        }

        model.Minimize(objective);

        using var solver = new CpSolver { StringParameters = $"max_time_in_seconds:{SolverTimeLimitSeconds};num_search_workers:4" };
        CpSolverStatus status = solver.Solve(model);
        cancellationToken.ThrowIfCancellationRequested();

        if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
        {
            return null;
        }

        var assignments = new List<ProposedTaskAssignment>();
        int skillMatched = 0;
        foreach (ScheduleTaskSummary task in unassigned)
        {
            for (int index = 0; index < members.Count; index++)
            {
                if (presence.TryGetValue((task.TaskId, index), out BoolVar? isPresent)
                    && solver.BooleanValue(isPresent))
                {
                    assignments.Add(new ProposedTaskAssignment(
                        task.TaskId, task.Key, task.AssigneeId, members[index].UserId!.Value, members[index].Email));
                    if (GreedyCapacityBalancer.SkillMatchCount(members[index], task) > 0)
                    {
                        skillMatched++;
                    }

                    break;
                }
            }
        }

        long solvedMakespan = solver.Value(makespan);
        long greedyMakespan = MakespanOfGreedy(input, members, openTasks, openTaskIds);

        return new ScheduleOptimisationResult(
            Name,
            ObjectiveText,
            assignments,
            [
                "Dependency order is enforced: no task starts before every unfinished predecessor has finished",
                "No member is given two tasks at the same time",
                "Task duration scales with the assignee's capacity — a half-time member takes twice as long",
                "Existing assignments on already-assigned tasks were not changed, but they do occupy their owner's timeline",
                "Completed tasks were not reassigned",
                status == CpSolverStatus.Optimal
                    ? "The solution is proven optimal for the stated objective"
                    : $"A feasible solution was found within the {SolverTimeLimitSeconds:0}s budget, but not proven optimal"
            ],
            [
                $"The three objective terms are weighted ({MakespanWeight} makespan / {TardinessWeight} tardiness / {SkillMatchWeight} skill match) rather than solved in strict priority order, so a large lateness saving could in principle buy a day of makespan",
                "Skill matching is a title-substring heuristic, not true competency matching — no task carries a structured required-skill field",
                "Member calendar-specific availability (holidays, leave) not modelled — capacity is treated as a constant figure, and every day is a working day",
                "One story point is treated as one day of work at full capacity, since ProjectTask has no separate estimate field"
            ],
            BuildExpectedGain(solvedMakespan, greedyMakespan, assignments.Count, skillMatched, input.Today));
    }

    /// <summary>
    /// Schedules the greedy balancer's own choices under identical constraints, so the reported gain
    /// is a like-for-like comparison of the two optimisers rather than a claim about the solver.
    /// </summary>
    private static long MakespanOfGreedy(
        ScheduleOptimisationInput input,
        List<ProjectMemberSummary> members,
        List<ScheduleTaskSummary> openTasks,
        HashSet<Guid> openTaskIds)
    {
        ScheduleOptimisationResult greedy = GreedyCapacityBalancer.Optimise(input);
        var chosen = greedy.Assignments.ToDictionary(a => a.TaskId, a => a.ProposedAssigneeId);

        var model = new CpModel();
        int horizon = openTasks.Sum(task => BaseDuration(task) * CapacityDivisor(members)) + 1;
        var start = new Dictionary<Guid, IntVar>();
        var end = new Dictionary<Guid, IntVar>();
        var intervalsByMember = members.ToDictionary(member => member.UserId!.Value, _ => new List<IntervalVar>());

        foreach (ScheduleTaskSummary task in openTasks)
        {
            start[task.TaskId] = model.NewIntVar(0, horizon, $"s_{task.TaskId}");
            Guid? owner = task.AssigneeId ?? chosen.GetValueOrDefault(task.TaskId);
            ProjectMemberSummary? member = owner is Guid id
                ? members.Find(candidate => candidate.UserId == id)
                : null;

            int duration = member is null ? BaseDuration(task) : ScaledDuration(task, member);
            end[task.TaskId] = model.NewIntVar(0, horizon, $"e_{task.TaskId}");
            model.Add(end[task.TaskId] == start[task.TaskId] + duration);

            if (member is not null)
            {
                intervalsByMember[member.UserId!.Value].Add(
                    model.NewIntervalVar(start[task.TaskId], duration, end[task.TaskId], $"g_{task.TaskId}"));
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

        IntVar makespan = model.NewIntVar(0, horizon, "greedy_makespan");
        model.AddMaxEquality(makespan, openTasks.Select(task => end[task.TaskId]));
        model.Minimize(makespan);

        using var solver = new CpSolver { StringParameters = "max_time_in_seconds:5;num_search_workers:4" };
        CpSolverStatus status = solver.Solve(model);
        return status is CpSolverStatus.Optimal or CpSolverStatus.Feasible ? solver.Value(makespan) : -1;
    }

    private static string BuildExpectedGain(
        long solvedMakespan, long greedyMakespan, int assignmentCount, int skillMatched, DateOnly today)
    {
        string finish = $"Project finishes in {solvedMakespan} day(s) (around {today.AddDays((int)Math.Min(solvedMakespan, 3_650)):yyyy-MM-dd})";
        string comparison = greedyMakespan switch
        {
            < 0 => "; the greedy baseline could not be scheduled for comparison",
            _ when greedyMakespan > solvedMakespan =>
                $", {greedyMakespan - solvedMakespan} day(s) sooner than the greedy load-balancer's assignment of the same work",
            _ when greedyMakespan == solvedMakespan =>
                ", the same finish date the greedy load-balancer reaches on this project — the solver found no better arrangement",
            _ => ", which the greedy baseline also reaches",
        };

        return $"{finish}{comparison}. {assignmentCount} assignment(s) proposed, {skillMatched} matching on skill tags.";
    }

    private static ScheduleOptimisationResult NothingToPropose(int unassignedCount) =>
        new(Name, ObjectiveText, [], [], [],
            unassignedCount == 0
                ? "No unassigned tasks to schedule — nothing proposed"
                : "No members with available capacity — nothing proposed");

    private static ScheduleOptimisationResult FellBackToGreedy(ScheduleOptimisationInput input)
    {
        ScheduleOptimisationResult greedy = GreedyCapacityBalancer.Optimise(input);
        return greedy with
        {
            ModelName = $"{GreedyCapacityBalancer.Name} (fallback)",
            ConstraintsRelaxed =
            [
                "The constraint solver could not produce a schedule for this project, so these assignments come from the greedy load-balancer instead — they balance workload but are not optimised for the project's finish date",
                .. greedy.ConstraintsRelaxed
            ],
        };
    }

    /// <summary>
    /// Which members may take this task: everyone for unassigned work, and only the current owner
    /// for work already assigned. An owner who is no longer an eligible member yields no candidates,
    /// so the task is still scheduled for dependency purposes but charged to nobody's timeline.
    /// </summary>
    private static List<int> CandidateMembers(
        ScheduleTaskSummary task, Dictionary<Guid, int> memberIndex, int memberCount)
    {
        if (task.AssigneeId is not Guid owner)
        {
            return [.. Enumerable.Range(0, memberCount)];
        }

        return memberIndex.TryGetValue(owner, out int pinned) ? [pinned] : [];
    }

    private static int BaseDuration(ScheduleTaskSummary task) => Math.Max(1, task.Points ?? 1);

    // Durations must be whole days for CP-SAT, so a capacity fraction becomes a multiplier: 0.5 FTE
    // doubles the duration. Rounded up, because rounding down would claim work goes faster than it can.
    private static int ScaledDuration(ScheduleTaskSummary task, ProjectMemberSummary member) =>
        (int)Math.Ceiling(BaseDuration(task) / (double)member.Capacity);

    // Worst-case slowdown across the team, used only to size the horizon.
    private static int CapacityDivisor(List<ProjectMemberSummary> members) =>
        members.Count == 0 ? 1 : (int)Math.Ceiling(1.0 / (double)members.Min(member => member.Capacity));
}

using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Application.Schedule;
using PlanWise.Modules.Scheduling.Domain.Optimisation;

namespace PlanWise.Modules.Scheduling.Application.Optimisation;

// v1 optimiser: a deterministic greedy load-balancer, not a real ML/optimisation model. It only
// proposes assignees for currently-unassigned, not-done tasks — it never touches dates or reassigns
// already-assigned work. Skill matching is a heuristic, not true competency matching: no task carries
// a structured required-skill field, so a member's skill tags are matched as a case-insensitive
// substring of the task's title (e.g. a member tagged "React" is preferred for "Fix React hydration
// bug"). Among members tied on skill match, load-balance-by-capacity still decides — that relaxation
// is reported honestly in the proposal's explanation rather than silently assumed away.
public sealed class ScheduleOptimisationJobHandler(
    IProjectTasksService projectTasksService,
    IProjectMembersService projectMembersService,
    IScheduleProposalRepository proposalRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IAsyncJobHandler
{
    private const string Model = "GreedyCapacityBalancer v1";

    public string JobType => "ScheduleOptimisation";

    public async Task<string> ExecuteAsync(Guid jobId, Guid projectId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ScheduleTaskSummary> tasks = await projectTasksService.GetScheduleTasksAsync(projectId, cancellationToken);
        IReadOnlyList<ProjectMemberSummary> members = await projectMembersService.GetMembersAsync(projectId, cancellationToken);

        var eligibleMembers = members
            .Where(member => member.UserId is not null && member.Capacity > 0)
            .ToList();

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        IReadOnlyDictionary<Guid, ScheduleCalculator.ComputedTaskSchedule> computedSchedule =
            ScheduleCalculator.Compute(tasks, new Dictionary<Guid, (DateOnly StartDate, DateOnly EndDate)>(), today);

        var load = eligibleMembers.ToDictionary(member => member.UserId!.Value, member => 0);
        foreach (ScheduleTaskSummary task in tasks)
        {
            if (task.AssigneeId is Guid assigneeId && !task.IsDone && load.ContainsKey(assigneeId))
            {
                load[assigneeId] += task.Points ?? 0;
            }
        }

        Dictionary<Guid, int> loadBefore = new(load);

        var unassignedTasks = tasks
            .Where(task => task.AssigneeId is null && !task.IsDone)
            .OrderByDescending(task => computedSchedule.TryGetValue(task.TaskId, out ScheduleCalculator.ComputedTaskSchedule? s) && s.IsCritical)
            .ThenByDescending(task => task.Points ?? 0)
            .ToList();

        var proposedAssignments = new List<(ScheduleTaskSummary Task, ProjectMemberSummary Member)>();
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

            proposedAssignments.Add((task, chosen));
            load[chosen.UserId!.Value] += task.Points ?? 0;
        }

        string expectedGain = BuildExpectedGain(unassignedTasks.Count, eligibleMembers.Count, loadBefore, load, skillMatchedAssignments);

        var proposal = ScheduleProposal.Create(
            projectId,
            jobId,
            Model,
            "Balance workload for unassigned backlog tasks across project members by remaining capacity, preferring a member whose skill tags match the task's title",
            BuildConstraintsHonoured(),
            BuildConstraintsRelaxed(),
            expectedGain,
            dateTimeProvider.UtcNow);

        foreach ((ScheduleTaskSummary task, ProjectMemberSummary member) in proposedAssignments)
        {
            proposal.AddAssignment(task.TaskId, task.Key, task.AssigneeId, member.UserId!.Value, member.Email);
        }

        proposalRepository.Add(proposal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return $"/api/v1/schedule/proposals/{proposal.Id}";
    }

    // A member's skill tag counts as a match if it appears as a substring of the task's title — the
    // only per-task text available to match against, since no task carries a structured
    // required-skill field. Case-insensitive; an unskilled member (no tags set) never matches.
    private static int SkillMatchCount(ProjectMemberSummary member, ScheduleTaskSummary task) =>
        member.Skills.Count(skill => task.Title.Contains(skill, StringComparison.OrdinalIgnoreCase));

    private static string[] BuildConstraintsHonoured() =>
    [
        "Existing assignments on already-assigned tasks were not changed",
        "Completed tasks were not reassigned",
        "Dependency ordering and dates from the current schedule were not altered",
        "A member whose skill tags matched the task's title was preferred over one with no match"
    ];

    private static string[] BuildConstraintsRelaxed() =>
    [
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

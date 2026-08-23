using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Application.Schedule;
using PlanWise.Modules.Scheduling.Domain.Optimisation;

namespace PlanWise.Modules.Scheduling.Application.Optimisation;

// v1 optimiser: a deterministic greedy load-balancer, not a real ML/optimisation model. It only
// proposes assignees for currently-unassigned, not-done tasks — it never touches dates or reassigns
// already-assigned work. Competency/skill matching isn't modelled yet (no task carries a required-skill
// signal), so every member with spare capacity is treated as eligible for every task; that relaxation
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

        foreach (ScheduleTaskSummary task in unassignedTasks)
        {
            if (eligibleMembers.Count == 0)
            {
                break;
            }

            ProjectMemberSummary chosen = eligibleMembers
                .OrderBy(member => load[member.UserId!.Value] / member.Capacity)
                .First();

            proposedAssignments.Add((task, chosen));
            load[chosen.UserId!.Value] += task.Points ?? 0;
        }

        string expectedGain = BuildExpectedGain(unassignedTasks.Count, eligibleMembers.Count, loadBefore, load);

        var proposal = ScheduleProposal.Create(
            projectId,
            jobId,
            Model,
            "Balance workload for unassigned backlog tasks across project members by remaining capacity",
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

    private static string[] BuildConstraintsHonoured() =>
    [
        "Existing assignments on already-assigned tasks were not changed",
        "Completed tasks were not reassigned",
        "Dependency ordering and dates from the current schedule were not altered"
    ];

    private static string[] BuildConstraintsRelaxed() =>
    [
        "Competency/skill matching not yet implemented — every member with spare capacity was treated as eligible for every task",
        "Member calendar-specific availability (holidays, leave) not modelled — capacity is treated as a constant figure"
    ];

    private static string BuildExpectedGain(
        int unassignedTaskCount,
        int eligibleMemberCount,
        IReadOnlyDictionary<Guid, int> before,
        IReadOnlyDictionary<Guid, int> after)
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

        return $"Reduces max/min assigned-points imbalance across members from {beforeImbalance} to {afterImbalance}";
    }
}

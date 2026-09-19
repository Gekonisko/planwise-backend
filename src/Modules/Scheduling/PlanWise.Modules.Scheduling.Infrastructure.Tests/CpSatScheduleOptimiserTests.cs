using Microsoft.Extensions.Logging.Abstractions;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Optimisation;
using PlanWise.Modules.Scheduling.Infrastructure.Optimisation;

namespace PlanWise.Modules.Scheduling.Infrastructure.Tests;

/// <summary>
/// These tests also serve as the check that the OR-Tools native library loads at all — a managed
/// build succeeds whether or not the platform's native solver is present, so without exercising the
/// solver here the first evidence would be a production job quietly falling back to the greedy path.
/// </summary>
public sealed class CpSatScheduleOptimiserTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);
    private static readonly Guid AliceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BobId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static CpSatScheduleOptimiser Optimiser() => new(NullLogger<CpSatScheduleOptimiser>.Instance);

    [Fact]
    public async Task It_keeps_a_dependency_chain_on_the_member_who_can_finish_it_soonest()
    {
        // Alice is full-time; Bob at half capacity takes twice as long on the same task.
        //
        // T1 -> T2 is a chain, so those two can never run in parallel: whoever holds them, the second
        // waits for the first. T3 is independent. The only question that matters is who gets the
        // chain, and the greedy balancer cannot ask it — it balances points against capacity, one
        // task at a time, with no notion of when any of the work happens.
        ProjectMemberSummary[] members = [Member(AliceId, "alice@planwise.test", 1.0m), Member(BobId, "bob@planwise.test", 0.5m)];

        var first = Guid.NewGuid();
        ScheduleTaskSummary[] tasks =
        [
            Task(first, "PW-1", points: 4),
            Task(Guid.NewGuid(), "PW-2", points: 4, predecessors: [first]),
            Task(Guid.NewGuid(), "PW-3", points: 4),
        ];

        ScheduleOptimisationResult result = await Optimiser().OptimiseAsync(
            new ScheduleOptimisationInput(Guid.NewGuid(), Today, tasks, members));

        Assert.Equal(CpSatScheduleOptimiser.Name, result.ModelName);
        Assert.Equal(3, result.Assignments.Count);

        // Both halves of the chain go to the full-capacity member: 4 + 4 = 8 days, while the
        // independent task runs alongside on the slower one. Splitting the chain would force the
        // second half to wait for the first and then run at half speed.
        IEnumerable<string> chainOwners = result.Assignments
            .Where(assignment => assignment.TaskKey is "PW-1" or "PW-2")
            .Select(assignment => assignment.ProposedAssigneeEmail);

        Assert.All(chainOwners, owner => Assert.Equal("alice@planwise.test", owner));
    }

    [Fact]
    public async Task It_reports_how_it_compares_against_the_greedy_baseline()
    {
        // The expected-gain text is the only place a reader learns whether the solver bought
        // anything, so it has to be a like-for-like comparison and not a bare claim.
        ProjectMemberSummary[] members = [Member(AliceId, "alice@planwise.test", 1.0m), Member(BobId, "bob@planwise.test", 0.5m)];
        var first = Guid.NewGuid();
        ScheduleTaskSummary[] tasks =
        [
            Task(first, "PW-1", points: 4),
            Task(Guid.NewGuid(), "PW-2", points: 4, predecessors: [first]),
            Task(Guid.NewGuid(), "PW-3", points: 4),
        ];

        ScheduleOptimisationResult result = await Optimiser().OptimiseAsync(
            new ScheduleOptimisationInput(Guid.NewGuid(), Today, tasks, members));

        Assert.Contains("Project finishes in", result.ExpectedGain, StringComparison.Ordinal);
        Assert.Contains("greedy", result.ExpectedGain, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Work_that_is_already_assigned_is_never_reassigned_but_still_occupies_its_owner()
    {
        ProjectMemberSummary[] members = [Member(AliceId, "alice@planwise.test", 1.0m), Member(BobId, "bob@planwise.test", 1.0m)];
        ScheduleTaskSummary[] tasks =
        [
            Task(Guid.NewGuid(), "PW-1", points: 10, assignee: AliceId),
            Task(Guid.NewGuid(), "PW-2", points: 10, assignee: AliceId),
            Task(Guid.NewGuid(), "PW-3", points: 1),
        ];

        ScheduleOptimisationResult result = await Optimiser().OptimiseAsync(
            new ScheduleOptimisationInput(Guid.NewGuid(), Today, tasks, members));

        // Only the unassigned task is proposed...
        ProposedTaskAssignment proposed = Assert.Single(result.Assignments);
        Assert.Equal("PW-3", proposed.TaskKey);

        // ...and it goes to Bob, because Alice is already carrying 20 days of work. A balancer that
        // ignored existing assignments would have no reason to prefer either.
        Assert.Equal("bob@planwise.test", proposed.ProposedAssigneeEmail);
    }

    [Fact]
    public async Task A_finished_predecessor_does_not_hold_anything_up()
    {
        var done = Guid.NewGuid();
        ProjectMemberSummary[] members = [Member(AliceId, "alice@planwise.test", 1.0m)];
        ScheduleTaskSummary[] tasks =
        [
            Task(done, "PW-1", points: 5, isDone: true),
            Task(Guid.NewGuid(), "PW-2", points: 5, predecessors: [done]),
        ];

        ScheduleOptimisationResult result = await Optimiser().OptimiseAsync(
            new ScheduleOptimisationInput(Guid.NewGuid(), Today, tasks, members));

        ProposedTaskAssignment proposed = Assert.Single(result.Assignments);
        Assert.Equal("PW-2", proposed.TaskKey);
        // 5 points at full capacity, with nothing to wait for.
        Assert.Contains("finishes in 5 day(s)", result.ExpectedGain, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, false)]   // no members with capacity
    [InlineData(false, true)]   // nothing unassigned to place
    public async Task It_proposes_nothing_rather_than_guessing(bool dropMembers, bool assignEverything)
    {
        ProjectMemberSummary[] members = dropMembers ? [] : [Member(AliceId, "alice@planwise.test", 1.0m)];
        ScheduleTaskSummary[] tasks = [Task(Guid.NewGuid(), "PW-1", points: 3, assignee: assignEverything ? AliceId : null)];

        ScheduleOptimisationResult result = await Optimiser().OptimiseAsync(
            new ScheduleOptimisationInput(Guid.NewGuid(), Today, tasks, members));

        Assert.Empty(result.Assignments);
        Assert.Contains("nothing proposed", result.ExpectedGain, StringComparison.Ordinal);
    }

    [Fact]
    public void The_greedy_baseline_still_runs_and_is_still_honest_about_what_it_ignores()
    {
        // It stays registered and reachable so "the solver beats the heuristic by X" remains a claim
        // that can be checked rather than assumed.
        ProjectMemberSummary[] members = [Member(AliceId, "alice@planwise.test", 1.0m), Member(BobId, "bob@planwise.test", 1.0m)];
        ScheduleTaskSummary[] tasks = [Task(Guid.NewGuid(), "PW-1", points: 5), Task(Guid.NewGuid(), "PW-2", points: 5)];

        ScheduleOptimisationResult result = GreedyCapacityBalancer.Optimise(
            new ScheduleOptimisationInput(Guid.NewGuid(), Today, tasks, members));

        Assert.Equal(GreedyCapacityBalancer.Name, result.ModelName);
        Assert.Equal(2, result.Assignments.Count);
        Assert.Contains(result.ConstraintsRelaxed,
            relaxed => relaxed.Contains("no model of when the work would actually run", StringComparison.Ordinal));
    }

    private static ProjectMemberSummary Member(Guid id, string email, decimal capacity) =>
        new(id, email, capacity, [], "Engineer", 100m);

    private static ScheduleTaskSummary Task(
        Guid id, string key, int points, Guid[]? predecessors = null, Guid? assignee = null, bool isDone = false) =>
        new(id, Guid.Empty, key, $"Task {key}", isDone, points, null, predecessors ?? [], assignee);
}

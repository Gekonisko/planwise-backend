using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Abstractions;
using PlanWise.Modules.Scheduling.Infrastructure.Optimisation;

namespace PlanWise.Modules.Scheduling.Infrastructure.Tests;

/// <summary>
/// ScheduleEvaluator is the measuring instrument of the allocation study: every arm's result — the
/// solver's, the greedy balancer's and the agent's — is a number this class produced. An instrument
/// nobody checked is not evidence, so these tests pin the four rules the comparison rests on, plus
/// the one artefact that already invalidated a run of the study before it was caught.
/// </summary>
public sealed class ScheduleEvaluatorTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);
    private static readonly Guid AliceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CarolId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid StrangerId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Two_tasks_on_one_member_run_one_after_the_other()
    {
        // The rule that makes allocation matter at all. Both tasks are independent, so a reader
        // might expect them to overlap; they cannot, because one person does one thing at a time.
        var one = Guid.NewGuid();
        var two = Guid.NewGuid();
        ScheduleTaskSummary[] tasks = [Task(one, "PW-1", points: 3), Task(two, "PW-2", points: 4)];

        AssignmentEvaluation together = ScheduleEvaluator.Evaluate(
            Input(tasks), new Dictionary<Guid, Guid> { [one] = AliceId, [two] = AliceId });

        AssignmentEvaluation split = ScheduleEvaluator.Evaluate(
            Input(tasks), new Dictionary<Guid, Guid> { [one] = AliceId, [two] = CarolId });

        Assert.Equal(7, together.MakespanDays);
        Assert.Equal(4, split.MakespanDays);
    }

    [Fact]
    public void A_dependency_chain_cannot_be_compressed_by_splitting_it()
    {
        // Two full-capacity members (Alice and Carol) and a two-link chain: the second waits for the first
        // whoever holds it, so spreading the chain buys nothing. This is the floor every allocation
        // method is measured against, and the reason a gap of zero is achievable but a negative one
        // is not.
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        ScheduleTaskSummary[] tasks = [Task(first, "PW-1", points: 3), Task(second, "PW-2", points: 3, predecessors: [first])];

        AssignmentEvaluation split = ScheduleEvaluator.Evaluate(
            Input(tasks), new Dictionary<Guid, Guid> { [first] = AliceId, [second] = CarolId });

        Assert.Equal(6, split.MakespanDays);
    }

    [Fact]
    public void Capacity_stretches_a_tasks_duration()
    {
        // Bob is half-time, so the same 4-point task costs him 8 days. An allocation that balances
        // story points rather than days gets this wrong, which is precisely what the study is
        // trying to detect in the LLM arms.
        var taskId = Guid.NewGuid();
        ScheduleTaskSummary[] tasks = [Task(taskId, "PW-1", points: 4)];

        AssignmentEvaluation toAlice = ScheduleEvaluator.Evaluate(
            Input(tasks), new Dictionary<Guid, Guid> { [taskId] = AliceId });
        AssignmentEvaluation toBob = ScheduleEvaluator.Evaluate(
            Input(tasks), new Dictionary<Guid, Guid> { [taskId] = BobId });

        Assert.Equal(4, toAlice.MakespanDays);
        Assert.Equal(8, toBob.MakespanDays);
    }

    [Fact]
    public void Work_left_unallocated_is_reported_rather_than_rewarded()
    {
        // The artefact that invalidated the first run of the allocation study. An unowned task
        // occupies nobody's timeline, so allocating nothing produces the *shortest* project of any
        // arm — a model that returned an empty assignment list scored better than the solver.
        //
        // The evaluator does not pretend otherwise; it reports the makespan it computed and names
        // every task still without an owner, and the study refuses to define a gap-to-optimum for
        // an incomplete allocation. Nothing downstream may read the makespan alone.
        var one = Guid.NewGuid();
        var two = Guid.NewGuid();
        ScheduleTaskSummary[] tasks = [Task(one, "PW-1", points: 5), Task(two, "PW-2", points: 5)];

        AssignmentEvaluation nothing = ScheduleEvaluator.Evaluate(Input(tasks), new Dictionary<Guid, Guid>());
        AssignmentEvaluation everything = ScheduleEvaluator.Evaluate(
            Input(tasks), new Dictionary<Guid, Guid> { [one] = AliceId, [two] = AliceId });

        Assert.Equal(2, nothing.UnassignedTaskKeys.Count);
        Assert.Equal(0, nothing.AssignedTasks);
        Assert.Empty(everything.UnassignedTaskKeys);

        // The trap, stated as an assertion so it cannot quietly come back: doing none of the work
        // really does look faster than doing all of it.
        Assert.True(nothing.MakespanDays < everything.MakespanDays);
    }

    [Fact]
    public void An_assignment_to_somebody_outside_the_project_is_rejected()
    {
        var taskId = Guid.NewGuid();
        ScheduleTaskSummary[] tasks = [Task(taskId, "PW-1", points: 3)];

        AssignmentEvaluation evaluation = ScheduleEvaluator.Evaluate(
            Input(tasks), new Dictionary<Guid, Guid> { [taskId] = StrangerId });

        Assert.Single(evaluation.Violations);
        Assert.Equal(["PW-1"], evaluation.UnassignedTaskKeys);
        Assert.Equal(0, evaluation.AssignedTasks);
    }

    [Fact]
    public void Finished_work_and_work_owned_by_somebody_else_are_not_reassigned()
    {
        // Two rules every arm is held to, checked together because they fail the same way: the
        // proposal is dropped, the violation is named, and no timeline is altered.
        var done = Guid.NewGuid();
        var owned = Guid.NewGuid();
        ScheduleTaskSummary[] tasks =
        [
            Task(done, "PW-1", points: 3, isDone: true),
            Task(owned, "PW-2", points: 3, assignee: AliceId),
        ];

        AssignmentEvaluation evaluation = ScheduleEvaluator.Evaluate(
            Input(tasks), new Dictionary<Guid, Guid> { [done] = BobId, [owned] = BobId });

        Assert.Equal(2, evaluation.Violations.Count);

        // PW-2 keeps Alice, and PW-1 does not come back from the dead onto anyone's plate.
        MemberLoad alice = evaluation.MemberLoads.Single(load => load.Email == "alice@planwise.test");
        MemberLoad bob = evaluation.MemberLoads.Single(load => load.Email == "bob@planwise.test");
        Assert.Equal(1, alice.TaskCount);
        Assert.Equal(0, bob.TaskCount);
    }

    private static ScheduleOptimisationInput Input(IReadOnlyList<ScheduleTaskSummary> tasks) =>
        new(Guid.NewGuid(), Today, tasks,
            [
                Member(AliceId, "alice@planwise.test", 1.0m),
                Member(BobId, "bob@planwise.test", 0.5m),
                Member(CarolId, "carol@planwise.test", 1.0m),
            ]);

    private static ProjectMemberSummary Member(Guid id, string email, decimal capacity) =>
        new(id, email, capacity, [], "Engineer", 100m);

    private static ScheduleTaskSummary Task(
        Guid id, string key, int points, Guid[]? predecessors = null, Guid? assignee = null, bool isDone = false) =>
        new(id, Guid.Empty, key, $"Task {key}", isDone, points, null, predecessors ?? [], assignee);
}

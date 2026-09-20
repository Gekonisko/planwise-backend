using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Abstractions;

namespace PlanWise.Research.LlmEvaluation;

/// <summary>
/// Synthetic resource-constrained allocation instances, fixed in code so every arm sees byte-identical
/// input and any run can be reproduced without a database.
///
/// They are built to make the allocation *matter*. On a backlog of independent one-point tasks handed
/// to equal members, every allocation finishes at the same time and the comparison measures nothing.
/// Each instance here therefore contains at least one of: a dependency chain long enough to dominate
/// the finish date, members whose capacity differs enough to change a task's duration outright, or
/// more parallel work than there are people to do it. Those are the three ways an allocation can be
/// wrong in days rather than in taste.
/// </summary>
internal static class AllocationScenarios
{
    private static readonly Guid ProjectId = Id(1);
    private static readonly DateOnly Today = new(2026, 1, 5);

    internal sealed record AllocationScenarioDefinition(
        string Id,
        string Description,
        ScheduleOptimisationInput Input,
        int Repetitions);

    public static IEnumerable<AllocationScenarioDefinition> All(int primaryRepetitions, int repetitions)
    {
        // AL-1 is the primary instance and carries the higher repetition count: it is the one whose
        // dispersion figures the study quotes, so it needs the sample size to support them.
        yield return new AllocationScenarioDefinition(
            "AL-1",
            "Customer Portal: 12 open tasks, one 5-task critical chain, 4 members of unequal capacity",
            CustomerPortal(),
            primaryRepetitions);

        yield return new AllocationScenarioDefinition(
            "AL-2",
            "Wide backlog: 20 independent tasks, 5 members — pure load balancing, no precedence to reason about",
            WideBacklog(),
            repetitions);

        yield return new AllocationScenarioDefinition(
            "AL-3",
            "Two chains: 16 tasks in two parallel dependency chains, 3 members — the chains must be split across people",
            TwoChains(),
            repetitions);

        yield return new AllocationScenarioDefinition(
            "AL-4",
            "Skewed capacity: 10 tasks, 4 members of whom two are quarter-time — duration depends on who takes the work",
            SkewedCapacity(),
            repetitions);

        yield return new AllocationScenarioDefinition(
            "AL-5",
            "Scale: 40 tasks with a mixed dependency forest, 6 members — the size at which a prompt stops fitting comfortably",
            AtScale(),
            repetitions);
    }

    /// <summary>
    /// The headline instance. A five-task chain (P-01 to P-05, 17 points) sets a floor on the finish
    /// date that no allocation can beat, and the chain runs faster on a full-capacity member — so the
    /// interesting decision is whether the chain goes to Dana (1.0) or is scattered across the
    /// part-timers while the independent work hogs the people who could have carried it.
    /// </summary>
    private static ScheduleOptimisationInput CustomerPortal()
    {
        var members = new List<ProjectMemberSummary>
        {
            Member(101, "dana@example.com", 1.0m, "Backend Engineer", ["api", "database"]),
            Member(102, "erik@example.com", 1.0m, "Frontend Engineer", ["ui", "portal"]),
            Member(103, "farah@example.com", 0.5m, "QA Engineer", ["test"]),
            Member(104, "gabor@example.com", 0.5m, "Backend Engineer", ["api"]),
        };

        var tasks = new List<ScheduleTaskSummary>
        {
            Task(1, "P-01", "Design the account database schema", 3, null, []),
            Task(2, "P-02", "Build the account api endpoints", 5, null, [1]),
            Task(3, "P-03", "Wire the portal ui to the account api", 4, null, [2]),
            Task(4, "P-04", "Test the account journey end to end", 3, null, [3]),
            Task(5, "P-05", "Harden the account api error handling", 2, null, [4]),

            Task(6, "P-06", "Static marketing page", 2, null, []),
            Task(7, "P-07", "Portal ui theme and typography", 3, null, []),
            Task(8, "P-08", "Database backup job", 2, null, []),
            Task(9, "P-09", "Test data seeding script", 1, null, []),
            Task(10, "P-10", "Api rate limiting", 3, null, []),
            Task(11, "P-11", "Accessibility pass on the portal ui", 2, null, []),
            Task(12, "P-12", "Logging and correlation ids", 2, null, []),
        };

        return new ScheduleOptimisationInput(ProjectId, Today, tasks, members);
    }

    /// <summary>
    /// No precedence at all, so the finish date is decided purely by how evenly the points land
    /// against capacity. The optimum is a bin-packing answer, which is the case an LLM is most likely
    /// to approximate well by eye — included precisely because it should be the agent's best showing.
    /// </summary>
    private static ScheduleOptimisationInput WideBacklog()
    {
        var members = new List<ProjectMemberSummary>
        {
            Member(201, "hana@example.com", 1.0m, "Engineer", ["api"]),
            Member(202, "ivan@example.com", 1.0m, "Engineer", ["ui"]),
            Member(203, "jonas@example.com", 0.75m, "Engineer", ["database"]),
            Member(204, "kira@example.com", 0.5m, "Engineer", ["test"]),
            Member(205, "liam@example.com", 1.0m, "Engineer", []),
        };

        int[] points = [5, 3, 8, 2, 1, 5, 3, 2, 8, 1, 3, 5, 2, 3, 1, 8, 2, 5, 3, 1];
        var tasks = points
            .Select((point, index) => Task(index + 1, $"W-{index + 1:D2}", $"Independent work item {index + 1}", point, null, []))
            .ToList();

        return new ScheduleOptimisationInput(ProjectId, Today, tasks, members);
    }

    /// <summary>
    /// Two chains of eight tasks each and only three members. Putting both chains on one person
    /// doubles the finish date, because a member does one task at a time — the single most common way
    /// an allocation that looks balanced on paper is badly wrong in days.
    /// </summary>
    private static ScheduleOptimisationInput TwoChains()
    {
        var members = new List<ProjectMemberSummary>
        {
            Member(301, "mira@example.com", 1.0m, "Engineer", ["payments"]),
            Member(302, "noah@example.com", 1.0m, "Engineer", ["reporting"]),
            Member(303, "olga@example.com", 1.0m, "Engineer", []),
        };

        var tasks = new List<ScheduleTaskSummary>();
        for (int i = 0; i < 8; i++)
        {
            tasks.Add(Task(i + 1, $"A-{i + 1:D2}", $"Payments pipeline step {i + 1}", 3, null, i == 0 ? [] : [i]));
        }

        for (int i = 0; i < 8; i++)
        {
            int index = i + 9;
            tasks.Add(Task(index, $"B-{i + 1:D2}", $"Reporting pipeline step {i + 1}", 3, null, i == 0 ? [] : [index - 1]));
        }

        return new ScheduleOptimisationInput(ProjectId, Today, tasks, members);
    }

    /// <summary>
    /// Capacity as a multiplier on duration, which is the rule most easily read and then not applied:
    /// a 4-point task is 4 days for Priya and 16 for Quinn. An allocation that balances *points*
    /// rather than days lands far from the optimum here, and does so for a reason the agent is told.
    /// </summary>
    private static ScheduleOptimisationInput SkewedCapacity()
    {
        var members = new List<ProjectMemberSummary>
        {
            Member(401, "priya@example.com", 1.0m, "Senior Engineer", ["api", "database"]),
            Member(402, "quinn@example.com", 0.25m, "Engineer", ["api"]),
            Member(403, "rosa@example.com", 1.0m, "Senior Engineer", ["ui"]),
            Member(404, "sami@example.com", 0.25m, "Engineer", ["ui"]),
        };

        var tasks = new List<ScheduleTaskSummary>
        {
            Task(1, "S-01", "Api contract for orders", 4, null, []),
            Task(2, "S-02", "Database migration for orders", 3, null, []),
            Task(3, "S-03", "Ui for order history", 5, null, []),
            Task(4, "S-04", "Api pagination", 2, null, []),
            Task(5, "S-05", "Ui empty states", 2, null, []),
            Task(6, "S-06", "Database index tuning", 3, null, []),
            Task(7, "S-07", "Api error contract", 2, null, []),
            Task(8, "S-08", "Ui loading skeletons", 1, null, []),
            Task(9, "S-09", "Order export endpoint", 4, null, [1]),
            Task(10, "S-10", "Order export screen", 3, null, [9]),
        };

        return new ScheduleOptimisationInput(ProjectId, Today, tasks, members);
    }

    /// <summary>
    /// Forty tasks over six members, with a dependency forest rather than a single chain. This is the
    /// size at which the briefing stops being small enough to hold in view and the number of possible
    /// allocations stops being enumerable — where a solver's advantage should show if it shows at all.
    /// </summary>
    private static ScheduleOptimisationInput AtScale()
    {
        var members = new List<ProjectMemberSummary>
        {
            Member(501, "tomas@example.com", 1.0m, "Engineer", ["api"]),
            Member(502, "uma@example.com", 1.0m, "Engineer", ["ui"]),
            Member(503, "viktor@example.com", 0.75m, "Engineer", ["database"]),
            Member(504, "wanda@example.com", 1.0m, "Engineer", ["test"]),
            Member(505, "yusuf@example.com", 0.5m, "Engineer", ["api"]),
            Member(506, "zara@example.com", 1.0m, "Engineer", []),
        };

        string[] areas = ["api", "ui", "database", "test", "search", "billing"];
        int[] pointCycle = [2, 3, 5, 1, 3, 8, 2, 1, 5, 3];

        var tasks = new List<ScheduleTaskSummary>();
        for (int index = 1; index <= 40; index++)
        {
            // Every fourth task depends on the one three places back: a forest of short chains rather
            // than one long spine, so no single path trivially fixes the answer.
            IReadOnlyList<int> predecessors = index % 4 == 0 && index > 3 ? [index - 3] : [];
            tasks.Add(Task(
                index,
                $"X-{index:D2}",
                $"{areas[index % areas.Length]} work item {index}",
                pointCycle[index % pointCycle.Length],
                null,
                predecessors));
        }

        return new ScheduleOptimisationInput(ProjectId, Today, tasks, members);
    }

    private static ScheduleTaskSummary Task(
        int index, string key, string title, int points, DateOnly? due, IReadOnlyList<int> predecessorIndexes) =>
        new(Id(1_000 + index), ProjectId, key, title, IsDone: false, points, due,
            predecessorIndexes.Select(i => Id(1_000 + i)).ToList(), AssigneeId: null);

    private static ProjectMemberSummary Member(
        int index, string email, decimal capacity, string role, IReadOnlyList<string> skills) =>
        new(Id(index), email, capacity, skills, role, HourlyRate: 100m);

    /// <summary>Deterministic ids, so a rerun produces the same instance and raw captures stay comparable.</summary>
    private static Guid Id(int seed)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, seed);
        return new Guid(bytes);
    }
}

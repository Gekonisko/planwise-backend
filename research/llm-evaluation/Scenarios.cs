using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Abstractions;

namespace PlanWise.Research.LlmEvaluation;

/// <summary>
/// Fixed synthetic projects. Every scenario is deterministic — identical bytes on every repetition —
/// because repeatability is measured by re-sending the same input, and any variation in the input
/// would be indistinguishable from variation in the model.
///
/// Ids are derived from the task key rather than random so that a rerun of the whole harness
/// produces comparable files.
/// </summary>
internal static class Scenarios
{
    private static Guid Id(string key)
    {
        Span<byte> bytes = stackalloc byte[16];
        System.Text.Encoding.UTF8.GetBytes(key.PadRight(16, '_'))[..16].CopyTo(bytes);
        return new Guid(bytes);
    }

    // A plausible mid-sized delivery: a customer portal rewrite. Titles and descriptions carry real
    // signal (integration work, compliance, migration) so the model has something to reason about
    // beyond the numbers.
    private static readonly (string Key, string Title, string Description, string Priority, int? Points, int? Value)[] Portal =
    [
        ("PW-1",  "Set up CI pipeline and staging environment", "Build, test and deploy pipeline with a staging slot; required before any feature work ships.", "High", 5, 40),
        ("PW-2",  "Design system and component library", "Shared Angular component library: buttons, forms, tables, dialogs, theming tokens.", "Medium", 8, 50),
        ("PW-3",  "User registration and login", "Email/password registration, login, refresh tokens, password reset over email.", "Urgent", 8, 90),
        ("PW-4",  "Role-based access control", "Per-project roles and a permission matrix enforced in the API and the UI.", "High", 13, 70),
        ("PW-5",  "Customer account dashboard", "Landing view after login: open orders, recent invoices, account health.", "High", 8, 85),
        ("PW-6",  "Invoice list and detail view", "Paged invoice list with filters, plus a detail view with downloadable PDF.", "Medium", 5, 65),
        ("PW-7",  "Payment provider integration", "Integrate Stripe for card payments including webhooks and reconciliation.", "Urgent", 13, 95),
        ("PW-8",  "Legacy data migration", "Migrate 400k customer records and 2M invoice lines from the legacy Oracle schema.", "High", 21, 60),
        ("PW-9",  "GDPR data export and deletion", "Self-service export of personal data and a verified deletion workflow.", "High", 8, 55),
        ("PW-10", "Audit log", "Append-only audit trail for account, payment and permission changes.", "Medium", 8, 45),
        ("PW-11", "Email notification service", "Transactional email with templating, retry and bounce handling.", "Medium", 5, 40),
        ("PW-12", "Support ticket submission", "Customers raise tickets from the portal; tickets sync to the existing helpdesk.", "Low", 5, 35),
        ("PW-13", "Search across invoices and orders", "Full-text search with filters over invoices and orders.", "Medium", 8, 50),
        ("PW-14", "Multi-currency support", "Display and settle invoices in EUR, USD and PLN with daily FX rates.", "Medium", 13, 60),
        ("PW-15", "Accessibility pass to WCAG 2.2 AA", "Keyboard navigation, contrast, screen-reader labels across all screens.", "Medium", 8, 45),
        ("PW-16", "Performance hardening of invoice list", "Invoice list must render under 1.5s at p95 for accounts with 10k+ invoices.", "Medium", 5, 40),
        ("PW-17", "Two-factor authentication", "TOTP-based second factor, enrolment and recovery codes.", "High", 8, 70),
        ("PW-18", "Admin back-office for support staff", "Internal screens for support agents to inspect and correct customer data.", "Low", 13, 30),
        ("PW-19", "Usage analytics and reporting", "Per-account usage dashboards with CSV export for finance.", "Low", 8, 35),
        ("PW-20", "Mobile responsive layout", "All customer-facing screens usable on phones from 360px width.", "Medium", 8, 50),
        ("PW-21", "Rate limiting and abuse protection", "Per-IP and per-account throttling on authentication and export endpoints.", "Medium", 5, 40),
        ("PW-22", "Observability: tracing and dashboards", "Distributed tracing, error dashboards and alerting on the payment path.", "Medium", 8, 45),
        ("PW-23", "Contract tests against the helpdesk API", "Consumer-driven contract tests so helpdesk changes cannot break the portal silently.", "Low", 5, 25),
        ("PW-24", "Decommission the legacy portal", "Redirect old URLs, archive the legacy app and shut down its infrastructure.", "Low", 8, 30),
    ];

    private static readonly RateCardMember[] TeamMembers =
    [
        new("anna@example.com", "Backend Developer", 1.0m, 95m),
        new("piotr@example.com", "Backend Developer", 0.5m, 105m),
        new("maria@example.com", "Frontend Developer", 1.0m, 90m),
        new("tomasz@example.com", "QA Engineer", 0.6m, 70m),
        new("ewa@example.com", "DevOps Engineer", 0.4m, 110m),
    ];

    /// <summary>The real provider blends rates by capacity; reproduced here so the input matches production shape.</summary>
    private static IReadOnlyList<RoleRate> FullRateCard()
    {
        return TeamMembers
            .GroupBy(member => member.Role, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                decimal capacity = group.Sum(member => member.Capacity);
                decimal blended = capacity > 0
                    ? Math.Round(group.Sum(member => member.HourlyRate * member.Capacity) / capacity, 2)
                    : Math.Round(group.Average(member => member.HourlyRate), 2);
                return new RoleRate(group.Key, blended, capacity, "USD", [.. group]);
            })
            .ToList();
    }

    /// <summary>What ProjectMemberRateCardProvider falls back to when nobody has a rate set.</summary>
    private static IReadOnlyList<RoleRate> PlaceholderRateCard() =>
        [new RoleRate("Team member", 75m, 1m, "USD", [])];

    private static IReadOnlyList<CostEstimationTaskSummary> CostTasks(int count, bool dropEstimates) =>
        Portal.Take(count)
            .Select((task, index) => new CostEstimationTaskSummary(
                Id(task.Key),
                task.Key,
                task.Title,
                task.Description,
                task.Priority,
                dropEstimates && index % 2 == 1 ? null : task.Points,
                IsDone: false,
                CompletedAtUtc: null))
            .ToList();

    public static IReadOnlyList<CostScenarioDefinition> CostScenarios(int primaryRepetitions, int repetitions) =>
    [
        new("CE-1", "maly backlog, pelny cennik", primaryRepetitions,
            new CostEstimationPrompt("Customer Portal", "Northwind Ltd", "USD", CostTasks(8, false), FullRateCard(), true)),

        new("CE-2", "sredni backlog, pelny cennik", repetitions,
            new CostEstimationPrompt("Customer Portal", "Northwind Ltd", "USD", CostTasks(24, false), FullRateCard(), true)),

        new("CE-3", "cennik zastepczy (brak stawek w zespole)", repetitions,
            new CostEstimationPrompt("Customer Portal", "Northwind Ltd", "USD", CostTasks(24, false), PlaceholderRateCard(), false)),

        new("CE-4", "braki w oszacowaniach punktowych", repetitions,
            new CostEstimationPrompt("Customer Portal", "Northwind Ltd", "USD", CostTasks(24, true), FullRateCard(), true)),
    ];

    private static TaskInsightSummary InsightTask(
        (string Key, string Title, string Description, string Priority, int? Points, int? Value) task,
        IReadOnlyList<Guid> predecessors,
        int blocksCount,
        bool dropBusinessValue) =>
        new(
            Id(task.Key),
            Id("project"),
            task.Key,
            task.Title,
            "Backlog",
            task.Priority,
            task.Points,
            dropBusinessValue ? null : task.Value,
            DueDate: null,
            AssigneeId: null,
            SprintId: null,
            Rank: 0m,
            SubtaskTotal: 0,
            SubtaskDone: 0,
            PredecessorTaskIds: predecessors,
            BlocksCount: blocksCount,
            CompletedAtUtc: null);

    // Dependency edges used by BP-4, expressed as "successor waits on predecessor".
    private static readonly (string Successor, string Predecessor)[] Edges =
    [
        ("PW-3", "PW-1"), ("PW-4", "PW-3"), ("PW-5", "PW-2"), ("PW-5", "PW-3"),
        ("PW-6", "PW-5"), ("PW-7", "PW-3"), ("PW-9", "PW-8"), ("PW-10", "PW-4"),
        ("PW-13", "PW-6"), ("PW-14", "PW-7"), ("PW-17", "PW-3"), ("PW-24", "PW-8"),
    ];

    private static IReadOnlyList<TaskInsightSummary> BacklogTasks(int count, bool withEdges, bool dropBusinessValue)
    {
        var chosen = Portal.Take(count).ToList();
        var keys = chosen.Select(task => task.Key).ToHashSet(StringComparer.Ordinal);
        var edges = withEdges
            ? Edges.Where(edge => keys.Contains(edge.Successor) && keys.Contains(edge.Predecessor)).ToList()
            : [];

        return chosen
            .Select(task => InsightTask(
                task,
                [.. edges.Where(edge => edge.Successor == task.Key).Select(edge => Id(edge.Predecessor))],
                edges.Count(edge => edge.Predecessor == task.Key),
                dropBusinessValue))
            .ToList();
    }

    /// <summary>Fixed pseudo-risk figures: the prioritiser is told risk, so it must be identical across repetitions.</summary>
    private static IReadOnlyDictionary<Guid, decimal> RiskScores(IReadOnlyList<TaskInsightSummary> tasks)
    {
        var scores = new Dictionary<Guid, decimal>();
        for (int i = 0; i < tasks.Count; i++)
        {
            scores[tasks[i].TaskId] = Math.Round(0.15m + (i * 7 % 17) * 0.04m, 2);
        }

        return scores;
    }

    public static IReadOnlyList<PrioritisationScenarioDefinition> PrioritisationScenarios(int primaryRepetitions, int repetitions)
    {
        var small = BacklogTasks(10, false, false);
        var medium = BacklogTasks(24, false, false);
        var noRisk = BacklogTasks(24, false, false);
        var chained = BacklogTasks(20, true, false);

        return
        [
            new("BP-1", "maly backlog, pelne dane", primaryRepetitions,
                new PrioritisationInput(Id("project"), small, RiskScores(small))),

            new("BP-2", "sredni backlog, pelne dane", repetitions,
                new PrioritisationInput(Id("project"), medium, RiskScores(medium))),

            new("BP-3", "brak prognozy ryzyka", repetitions,
                new PrioritisationInput(Id("project"), noRisk, new Dictionary<Guid, decimal>())),

            new("BP-4", "lancuchy zaleznosci", repetitions,
                new PrioritisationInput(Id("project"), chained, RiskScores(chained))),
        ];
    }

    /// <summary>Dependency edges of a scenario, as task-key pairs, for the consistency check.</summary>
    public static IReadOnlyList<(string Successor, string Predecessor)> EdgesOf(PrioritisationInput input)
    {
        var keysById = input.BacklogTasks.ToDictionary(task => task.TaskId, task => task.Key);
        return input.BacklogTasks
            .SelectMany(task => task.PredecessorTaskIds
                .Where(keysById.ContainsKey)
                .Select(id => (Successor: task.Key, Predecessor: keysById[id])))
            .ToList();
    }
}

internal sealed record CostScenarioDefinition(string Id, string Description, int Repetitions, CostEstimationPrompt Prompt);

internal sealed record PrioritisationScenarioDefinition(string Id, string Description, int Repetitions, PrioritisationInput Input);

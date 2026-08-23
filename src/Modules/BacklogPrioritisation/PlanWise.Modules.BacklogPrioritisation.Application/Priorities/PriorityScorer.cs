using System.Globalization;
using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities;

// Deterministic weighted scorecard, not a trained model — same honesty as RiskPrediction's
// RiskScorer. Value/dependency/complexity/risk are each normalised to 0..1 against the current
// backlog's own range (not a global scale), then combined with fixed weights. Complexity is scored
// as "bigger = more complex" but weighted negatively (a simple quick win beats a sprawling task all
// else equal); risk is weighted positively on the theory that de-risking early is good triage —
// both are judgement calls a real model would learn, not something this heuristic derives.
internal static class PriorityScorer
{
    private const decimal ValueWeight = 0.40m;
    private const decimal DependencyWeight = 0.25m;
    private const decimal ComplexityWeight = 0.15m;
    private const decimal RiskWeight = 0.20m;
    private const decimal NeutralScore = 0.5m;

    public sealed record ScoredTask(
        TaskInsightSummary Task,
        decimal ValueScore,
        decimal DependencyScore,
        decimal ComplexityScore,
        decimal RiskScore,
        string Reason);

    public static IReadOnlyList<ScoredTask> Score(
        IReadOnlyList<TaskInsightSummary> backlogTasks,
        IReadOnlyDictionary<Guid, decimal> riskScores)
    {
        if (backlogTasks.Count == 0)
        {
            return [];
        }

        int maxBusinessValue = Math.Max(1, backlogTasks.Max(task => task.BusinessValue ?? 0));
        int maxPoints = Math.Max(1, backlogTasks.Max(task => task.Points ?? 0));
        int maxBlocks = Math.Max(1, backlogTasks.Max(task => task.BlocksCount));

        return backlogTasks
            .Select(task => BuildScoredTask(task, riskScores, maxBusinessValue, maxPoints, maxBlocks))
            .OrderByDescending(scored => Weighted(scored))
            .ToList();
    }

    private static ScoredTask BuildScoredTask(
        TaskInsightSummary task,
        IReadOnlyDictionary<Guid, decimal> riskScores,
        int maxBusinessValue,
        int maxPoints,
        int maxBlocks)
    {
        decimal valueScore = task.BusinessValue is int businessValue ? (decimal)businessValue / maxBusinessValue : NeutralScore;
        decimal dependencyScore = (decimal)task.BlocksCount / maxBlocks;
        decimal complexityScore = task.Points is int points ? (decimal)points / maxPoints : NeutralScore;
        decimal riskScore = riskScores.TryGetValue(task.TaskId, out decimal risk) ? risk : NeutralScore;

        string reason = BuildReason(valueScore, dependencyScore, complexityScore, riskScore, task.BlocksCount);

        return new ScoredTask(task, valueScore, dependencyScore, complexityScore, riskScore, reason);
    }

    private static decimal Weighted(ScoredTask scored) =>
        ValueWeight * scored.ValueScore +
        DependencyWeight * scored.DependencyScore +
        ComplexityWeight * (1 - scored.ComplexityScore) +
        RiskWeight * scored.RiskScore;

    private static string BuildReason(decimal valueScore, decimal dependencyScore, decimal complexityScore, decimal riskScore, int blocksCount)
    {
        var drivers = new List<(string Label, decimal Weight)>
        {
            ("high business value", ValueWeight * valueScore),
            ("unblocks other work", DependencyWeight * dependencyScore),
            ("low complexity", ComplexityWeight * (1 - complexityScore)),
            ("elevated slip risk", RiskWeight * riskScore)
        };

        IEnumerable<string> topLabels = drivers.OrderByDescending(driver => driver.Weight).Take(2).Select(driver => driver.Label);
        string labelText = string.Join(", ", topLabels);

        return blocksCount > 0
            ? $"{labelText} ({blocksCount.ToString(CultureInfo.InvariantCulture)} downstream task(s) blocked)"
            : labelText;
    }
}

using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Modules.RiskPrediction.Application.Risks;

// Deterministic weighted heuristic scorecard, not a trained model: there is no historical slip data
// in this system to fit or validate against (see RiskAssessmentJobHandler.Assumptions), so the
// weights below are fixed, illustrative point values rather than learned coefficients. Each factor
// that fires is reported as a FeatureContribution so the explanation drawer can show its own math.
internal static class RiskScorer
{
    private const decimal OverdueWeight = 0.35m;
    private const decimal DueSoonWeight = 0.20m;
    private const int DueSoonDays = 3;
    private const decimal PerBlockerWeight = 0.10m;
    private const decimal MaxBlockerWeight = 0.30m;
    private const decimal UnassignedWeight = 0.15m;
    private const int LargeScopeThresholdPoints = 8;
    private const decimal PerExtraPointWeight = 0.05m;
    private const decimal MaxLargeScopeWeight = 0.20m;
    private const decimal MaxProbability = 0.95m;

    public sealed record FeatureContribution(string Feature, decimal Weight, string Detail);

    public sealed record ScoreResult(decimal Probability, int DayImpact, string Reason, IReadOnlyList<FeatureContribution> Features);

    public static ScoreResult Score(TaskInsightSummary task, IReadOnlyDictionary<Guid, TaskInsightSummary> allTasksById, DateOnly today)
    {
        var features = new List<FeatureContribution>();
        decimal score = 0m;

        if (task.DueDate is DateOnly dueDate)
        {
            int daysUntilDue = dueDate.DayNumber - today.DayNumber;
            if (daysUntilDue < 0)
            {
                score += OverdueWeight;
                features.Add(new FeatureContribution("Overdue", OverdueWeight, $"Due date was {-daysUntilDue} day(s) ago"));
            }
            else if (daysUntilDue <= DueSoonDays)
            {
                score += DueSoonWeight;
                features.Add(new FeatureContribution("DueSoon", DueSoonWeight, $"Due in {daysUntilDue} day(s)"));
            }
        }

        int openBlockers = task.PredecessorTaskIds.Count(id =>
            allTasksById.TryGetValue(id, out TaskInsightSummary? predecessor) && predecessor.Status != "Done");
        if (openBlockers > 0)
        {
            decimal weight = Math.Min(PerBlockerWeight * openBlockers, MaxBlockerWeight);
            score += weight;
            features.Add(new FeatureContribution("BlockedByOpenDependencies", weight, $"{openBlockers} predecessor task(s) not yet done"));
        }

        if (task.AssigneeId is null)
        {
            score += UnassignedWeight;
            features.Add(new FeatureContribution("Unassigned", UnassignedWeight, "No assignee"));
        }

        if (task.Points is int points && points >= LargeScopeThresholdPoints)
        {
            decimal weight = Math.Min(PerExtraPointWeight * (points - LargeScopeThresholdPoints + 3), MaxLargeScopeWeight);
            score += weight;
            features.Add(new FeatureContribution("LargeScope", weight, $"{points} points"));
        }

        decimal probability = Math.Clamp(score, 0m, MaxProbability);
        int dayImpact = (int)Math.Round(probability * BaselineSlipDays(task.Points), MidpointRounding.AwayFromZero);

        return new ScoreResult(probability, dayImpact, BuildReason(features), features);
    }

    private static int BaselineSlipDays(int? points) => points is int value ? Math.Max(1, value / 2) : 3;

    private static string BuildReason(List<FeatureContribution> features)
    {
        if (features.Count == 0)
        {
            return "No elevated risk factors detected";
        }

        IEnumerable<string> topDetails = features
            .OrderByDescending(feature => feature.Weight)
            .Take(2)
            .Select(feature => feature.Detail);

        return string.Join("; ", topDetails);
    }
}

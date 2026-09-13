using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.RiskPrediction.Application.Abstractions;

namespace PlanWise.Modules.RiskPrediction.Application.Risks;

// Maps a PlanWise task onto the five features the model was fitted on, and scores it.
//
// The mapping is the part that can silently go wrong, so it is stated explicitly:
//
//   story_point          <- task.Points
//   blocking_links       <- PredecessorTaskIds.Count + BlocksCount. TAWOS counts an issue's blocking
//                           links without regard to direction, so both sides are summed here rather
//                           than only counting predecessors.
//   sprint_issue_count   <- tasks committed to the same sprint
//   sprint_story_points  <- their total points
//   sprint_length_days   <- the sprint's end minus its start
//
// Two of the study's structural features are deliberately absent because PlanWise cannot produce
// them: days_in_backlog (nothing in Delivery records when a task was created) and assoc_links (no
// count of non-blocking links). Training on features that cannot be served is what produces a model
// that looks good offline and quietly degrades in production; the cost of leaving them out was
// measured instead, at 0.011 ROC AUC.
internal static class TrainedSlipScorer
{
    public sealed record SprintAggregate(int IssueCount, int StoryPoints, int LengthDays);

    public static IReadOnlyDictionary<Guid, SprintAggregate> AggregateSprints(
        IReadOnlyList<SprintInsightSummary> sprints,
        IReadOnlyList<TaskInsightSummary> tasks)
    {
        var tasksBySprint = tasks
            .Where(task => task.SprintId is not null)
            .GroupBy(task => task.SprintId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        return sprints.ToDictionary(
            sprint => sprint.SprintId,
            sprint =>
            {
                List<TaskInsightSummary> committed = tasksBySprint.GetValueOrDefault(sprint.SprintId, []);
                return new SprintAggregate(
                    committed.Count,
                    committed.Sum(task => task.Points ?? 0),
                    sprint.EndDate.DayNumber - sprint.StartDate.DayNumber);
            });
    }

    public static RiskScorer.ScoreResult Score(
        SlipModelArtifact model,
        TaskInsightSummary task,
        IReadOnlyDictionary<Guid, SprintAggregate> sprintAggregates)
    {
        SprintAggregate? sprint = task.SprintId is Guid sprintId
            ? sprintAggregates.GetValueOrDefault(sprintId)
            : null;

        // null means "this project cannot tell us", and the model's own training median is
        // substituted below. It is not the same as zero: a task with no sprint has an unknown sprint
        // size, not an empty one.
        var values = new Dictionary<string, double?>(StringComparer.Ordinal)
        {
            ["story_point"] = task.Points,
            ["blocking_links"] = task.PredecessorTaskIds.Count + task.BlocksCount,
            ["sprint_issue_count"] = sprint?.IssueCount,
            ["sprint_story_points"] = sprint?.StoryPoints,
            ["sprint_length_days"] = sprint?.LengthDays,
        };

        Scored scored = ScoreVector(model, values);

        // The model predicts *whether* a task slips, never by how much — TAWOS has no such label. Day
        // impact stays the scorecard's heuristic, and the assumptions say so rather than letting the
        // number borrow the model's credibility.
        int dayImpact = (int)Math.Round(
            scored.Probability * BaselineSlipDays(task.Points), MidpointRounding.AwayFromZero);

        return new RiskScorer.ScoreResult(
            scored.Probability, dayImpact, BuildReason(scored.Contributions), scored.Contributions);
    }

    /// <param name="Logit">
    /// Exposed so a test can assert that the contributions really do sum to it. If they ever stop
    /// doing so the explanation drawer is showing decorative numbers rather than the model's actual
    /// reasoning, and nothing else in the system would notice.
    /// </param>
    public sealed record Scored(decimal Probability, IReadOnlyList<RiskFeatureContribution> Contributions, double Logit);

    /// <summary>
    /// The artifact's documented maths, and the only place it is implemented: impute the median when
    /// a value is null, clamp at zero, log1p, standardise, weight, sum, then Platt-scale.
    /// </summary>
    public static Scored ScoreVector(SlipModelArtifact model, IReadOnlyDictionary<string, double?> values)
    {
        double logit = model.Intercept;
        var contributions = new List<RiskFeatureContribution>(model.Features.Count);

        foreach (SlipModelFeature feature in model.Features)
        {
            if (!values.TryGetValue(feature.Name, out double? raw))
            {
                throw new InvalidOperationException(
                    $"The trained slip model expects a feature named '{feature.Name}', which this build does not know how to compute.");
            }

            bool imputed = raw is null;
            double value = raw ?? feature.Median;

            // Exactly the exporter's pipeline: clamp to non-negative (a sprint whose end precedes
            // its start would otherwise break log1p), log1p, standardise, weight.
            double standardised = (Math.Log(1.0 + Math.Max(0.0, value)) - feature.Mean) / feature.Scale;
            double contribution = feature.Coefficient * standardised;
            logit += contribution;

            contributions.Add(new RiskFeatureContribution(
                feature.Name,
                (decimal)Math.Round(contribution, 4),
                Describe(feature.Name, value, imputed)));
        }

        // Platt scaling, sklearn's sign convention: p = 1 / (1 + exp(a·f + b)).
        double calibrated = 1.0 / (1.0 + Math.Exp(model.Calibration.A * logit + model.Calibration.B));
        decimal probability = Math.Clamp((decimal)calibrated, 0m, 1m);

        var ranked = contributions.OrderByDescending(feature => Math.Abs(feature.Weight)).ToList();
        return new Scored(probability, ranked, logit);
    }

    private static int BaselineSlipDays(int? points) => points is int value ? Math.Max(1, value / 2) : 3;

    private static string Describe(string feature, double value, bool imputed)
    {
        string suffix = imputed ? " (not available for this task; the training median was used)" : string.Empty;
        return feature switch
        {
            "story_point" => $"{value:0.#} story point(s){suffix}",
            "blocking_links" => $"{value:0} dependency link(s){suffix}",
            "sprint_issue_count" => $"{value:0} task(s) in the sprint{suffix}",
            "sprint_story_points" => $"{value:0} point(s) committed to the sprint{suffix}",
            "sprint_length_days" => $"{value:0}-day sprint{suffix}",
            _ => $"{value:0.##}{suffix}",
        };
    }

    private static string BuildReason(IReadOnlyList<RiskFeatureContribution> ranked)
    {
        // Only factors pushing risk *up* are worth naming as a reason; a negative contribution is
        // the model arguing the task is safer than average.
        IEnumerable<string> raising = ranked
            .Where(feature => feature.Weight > 0)
            .Take(2)
            .Select(feature => feature.Detail);

        string reason = string.Join("; ", raising);
        return reason.Length > 0 ? reason : "No factor raises this task above the model's baseline";
    }
}

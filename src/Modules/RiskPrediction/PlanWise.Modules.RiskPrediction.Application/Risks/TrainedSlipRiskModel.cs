using System.Globalization;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.RiskPrediction.Application.Abstractions;

namespace PlanWise.Modules.RiskPrediction.Application.Risks;

// The fitted alternative to WeightedScorecardRiskModel: task slip probabilities come from a logistic
// regression trained on 19,251 sprint-committed issues across 36 open-source projects (TAWOS), not
// from hand-chosen weights.
//
// Only the *task* probability is learned. Sprint forecasting still runs through VelocityEstimator
// and SprintForecaster, unchanged and shared with the scorecard, because TAWOS carries no label for
// how late a slipped issue eventually was — so day impact and delivery dates remain projections
// rather than predictions. The assumptions returned with every run say this plainly; a model that
// is honest about which half of its output is fitted is worth more than one that is not.
//
// Both models stay registered and runnable so the comparison "the model beats the heuristic by X"
// can be measured on this system's own captured outcomes rather than asserted from the study.
public sealed class TrainedSlipRiskModel : IRiskPredictionModel
{
    private readonly SlipModelArtifact model = SlipModelArtifact.Instance;

    public string ModelName => model.ModelName;

    public Task<RiskPredictionResult> PredictAsync(RiskPredictionInput input, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<Guid, TrainedSlipScorer.SprintAggregate> sprintAggregates =
            TrainedSlipScorer.AggregateSprints(input.Sprints, input.Tasks);

        var taskRisks = input.Tasks
            .Where(task => task.Status != DoneStatus)
            .Select(task =>
            {
                RiskScorer.ScoreResult score = TrainedSlipScorer.Score(model, task, sprintAggregates);
                return new TaskRiskPrediction(
                    task.TaskId, task.Key, score.Probability, score.DayImpact, score.Reason, score.Features);
            })
            .ToList();

        decimal teamFullTimeEquivalents = input.Members
            .Where(member => member.UserId is not null)
            .Sum(member => member.Capacity);

        VelocityEstimator.Estimate velocity =
            VelocityEstimator.ForProject(input.Sprints, input.Tasks, teamFullTimeEquivalents);

        var sprintForecasts = input.Sprints
            .Where(sprint => sprint.State == ActiveState)
            .Select(sprint =>
            {
                var sprintTasks = input.Tasks.Where(task => task.SprintId == sprint.SprintId).ToList();
                SprintForecaster.ForecastResult forecast =
                    SprintForecaster.Forecast(sprint, sprintTasks, velocity.PointsPerDay, input.Today);
                return new SprintRiskForecast(
                    sprint.SprintId, forecast.CompletionProbability, forecast.ExpectedPoints,
                    forecast.P50DeliveryDate, forecast.P90DeliveryDate);
            })
            .ToList();

        int uncommitted = input.Tasks.Count(task => task.Status != DoneStatus && task.SprintId is null);

        return Task.FromResult(new RiskPredictionResult(
            taskRisks,
            sprintForecasts,
            model.Provenance.TrainingWindowDays,
            BuildAssumptions(velocity, uncommitted)));
    }

    private string[] BuildAssumptions(VelocityEstimator.Estimate velocity, int uncommittedTaskCount)
    {
        SlipModelProvenance provenance = model.Provenance;
        var assumptions = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture,
                $"Slip probabilities come from a logistic regression fitted to {provenance.Rows:N0} sprint-committed issues across {provenance.Projects} projects in {provenance.Dataset}, where {provenance.BaseRate:P1} of issues missed their sprint. It scores about 0.59 ROC AUC on projects it has never seen — better than chance, but far from certain; treat the ranking as more trustworthy than any individual number."),

            "The model was fitted on other organisations' projects, not on this one. Its probabilities have not been recalibrated against this system's own delivery outcomes, so they are best read as a way of ordering attention rather than as literal likelihoods.",

            "Only whether a task slips is modelled. How many days late it would be is not — the training data carries no such label — so the day-impact figure remains a heuristic (probability multiplied by a size-based baseline), not a prediction.",

            DescribeVelocity(velocity)
        };

        // Every training example was an issue already committed to a sprint, so a backlog task is
        // outside the distribution the model was fitted on. Say so, but only when it applies.
        if (uncommittedTaskCount > 0)
        {
            assumptions.Add(string.Create(CultureInfo.InvariantCulture,
                $"{uncommittedTaskCount:N0} scored task(s) are not committed to a sprint. Every training example was, so the sprint-shaped features fall back to training medians for these and their scores are correspondingly less reliable."));
        }

        return [.. assumptions];
    }

    // Shared wording with the scorecard's own assumption, and for the same reason: a rate observed
    // over real sprints and one assumed from headcount should never read the same.
    private static string DescribeVelocity(VelocityEstimator.Estimate velocity) =>
        velocity.IsMeasured
            ? string.Create(CultureInfo.InvariantCulture,
                $"Sprint completion forecasts are not model output. They project delivery at {velocity.PointsPerDay:0.##} story points per day, averaged over {velocity.ObservedSprints} completed sprint(s) in this project, and assume that rate holds.")
            : string.Create(CultureInfo.InvariantCulture,
                $"Sprint completion forecasts are not model output, and this project has no completed sprint to measure. They assume {velocity.PointsPerDay:0.##} story points per day, derived from team capacity at a nominal 0.6 points per full-time member per day.");

    private const string DoneStatus = "Done";
    private const string ActiveState = "Active";
}

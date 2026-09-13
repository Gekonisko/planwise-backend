using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.RiskPrediction.Application.Abstractions;

namespace PlanWise.Modules.RiskPrediction.Application.Risks;

// The baseline model: a deterministic weighted heuristic (RiskScorer + SprintForecaster), not a
// trained one. It is worth keeping once a fitted model exists — a hand-tuned scorecard is the
// control condition any learned model has to beat, and "the model beats the heuristic by X" is only
// a claim you can make if the heuristic is still runnable.
//
// Reports TrainingWindowDays = 0 and says so in its assumptions: there is no historical slip-outcome
// data in this system to fit or backtest against, so the weights are fixed illustrative values
// rather than learned coefficients.
//
// Synchronous work behind an async interface, hence the Task.FromResult — the seam is async because
// the implementations that replace this one (an inference call, a model-file load) will need it.
public sealed class WeightedScorecardRiskModel : IRiskPredictionModel
{
    private const string ScorecardAssumption =
        "Risk is scored by a deterministic weighted heuristic (due-date pressure, open blocking dependencies, missing assignee, task size), not a trained statistical or ML model.";

    private const string NoOutcomeDataAssumption =
        "No historical delivery data (actual slip outcomes) exists yet in this system, so the heuristic cannot be calibrated or backtested against real results — weights are fixed, illustrative estimates.";

    public string ModelName => "WeightedScorecard v1";

    public Task<RiskPredictionResult> PredictAsync(RiskPredictionInput input, CancellationToken cancellationToken = default)
    {
        var tasksById = input.Tasks.ToDictionary(task => task.TaskId);

        var taskRisks = input.Tasks
            .Where(task => task.Status != "Done")
            .Select(task =>
            {
                RiskScorer.ScoreResult score = RiskScorer.Score(task, tasksById, input.Today);
                return new TaskRiskPrediction(
                    task.TaskId,
                    task.Key,
                    score.Probability,
                    score.DayImpact,
                    score.Reason,
                    score.Features);
            })
            .ToList();

        // Only members linked to a real user count towards capacity — an invited-but-unregistered
        // member isn't delivering anything yet. This sums to full-time-equivalents, not points:
        // turning it into a delivery rate is VelocityEstimator's job.
        decimal teamFullTimeEquivalents = input.Members
            .Where(member => member.UserId is not null)
            .Sum(member => member.Capacity);

        VelocityEstimator.Estimate velocity =
            VelocityEstimator.ForProject(input.Sprints, input.Tasks, teamFullTimeEquivalents);

        var sprintForecasts = input.Sprints
            .Where(sprint => sprint.State == "Active")
            .Select(sprint =>
            {
                var sprintTasks = input.Tasks.Where(task => task.SprintId == sprint.SprintId).ToList();
                SprintForecaster.ForecastResult forecast =
                    SprintForecaster.Forecast(sprint, sprintTasks, velocity.PointsPerDay, input.Today);
                return new SprintRiskForecast(
                    sprint.SprintId,
                    forecast.CompletionProbability,
                    forecast.ExpectedPoints,
                    forecast.P50DeliveryDate,
                    forecast.P90DeliveryDate);
            })
            .ToList();

        string[] assumptions =
        [
            ScorecardAssumption,
            NoOutcomeDataAssumption,
            DescribeVelocity(velocity)
        ];

        return Task.FromResult(new RiskPredictionResult(taskRisks, sprintForecasts, TrainingWindowDays: 0, assumptions));
    }

    // The reader has to be able to tell a rate observed over real sprints from one that was assumed,
    // because the two deserve very different amounts of trust.
    private static string DescribeVelocity(VelocityEstimator.Estimate velocity) =>
        velocity.IsMeasured
            ? $"Sprint forecasts project delivery at {velocity.PointsPerDay:0.##} story points per day, averaged over {velocity.ObservedSprints} completed sprint(s) in this project. The rate is assumed constant: holidays, partial availability and mid-sprint scope changes are not modelled."
            : $"This project has no completed sprint that delivered points, so there is no measured velocity to project from. Sprint forecasts instead assume {velocity.PointsPerDay:0.##} story points per day, derived from team capacity at a nominal 0.6 points per full-time member per day — an assumption, not a measurement.";
}

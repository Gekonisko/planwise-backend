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
    private static readonly string[] FixedAssumptions =
    [
        "Risk is scored by a deterministic weighted heuristic (due-date pressure, open blocking dependencies, missing assignee, task size), not a trained statistical or ML model.",
        "No historical delivery data (actual slip outcomes) exists yet in this system, so the heuristic cannot be calibrated or backtested against real results — weights are fixed, illustrative estimates.",
        "Sprint forecasts assume each member's configured capacity is available every day of the sprint at a constant rate; holidays, partial availability and mid-sprint scope changes are not modelled."
    ];

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
        // member isn't delivering anything yet.
        decimal teamCapacityPoints = input.Members
            .Where(member => member.UserId is not null)
            .Sum(member => member.Capacity);

        var sprintForecasts = input.Sprints
            .Where(sprint => sprint.State == "Active")
            .Select(sprint =>
            {
                var sprintTasks = input.Tasks.Where(task => task.SprintId == sprint.SprintId).ToList();
                SprintForecaster.ForecastResult forecast =
                    SprintForecaster.Forecast(sprint, sprintTasks, teamCapacityPoints, input.Today);
                return new SprintRiskForecast(
                    sprint.SprintId,
                    forecast.CompletionProbability,
                    forecast.ExpectedPoints,
                    forecast.P50DeliveryDate,
                    forecast.P90DeliveryDate);
            })
            .ToList();

        return Task.FromResult(new RiskPredictionResult(taskRisks, sprintForecasts, TrainingWindowDays: 0, FixedAssumptions));
    }
}

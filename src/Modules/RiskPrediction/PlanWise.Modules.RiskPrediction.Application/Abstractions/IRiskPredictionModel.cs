using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Modules.RiskPrediction.Application.Abstractions;

// The risk-model boundary, mirroring ICostEstimationModel: Application depends only on this
// interface, never on how a prediction is actually produced. Today the only implementation is
// WeightedScorecardRiskModel (a deterministic heuristic); a trained model reads the same input and
// returns the same result, so swapping it in touches this module's Infrastructure registration and
// nothing else — not the job handler, not the persistence, not the API.

/// <summary>Everything a risk model is allowed to look at for one project.</summary>
/// <param name="Today">
/// Passed in rather than read from a clock inside the model, so a model is a pure function of its
/// input and can be replayed or backtested against a historical date.
/// </param>
public sealed record RiskPredictionInput(
    Guid ProjectId,
    DateOnly Today,
    IReadOnlyList<TaskInsightSummary> Tasks,
    IReadOnlyList<SprintInsightSummary> Sprints,
    IReadOnlyList<ProjectMemberSummary> Members);

/// <param name="Weight">
/// How much this factor moved the probability. A scorecard reports its fixed point value here; a
/// trained model would report a per-feature attribution (e.g. a SHAP value).
/// </param>
public sealed record RiskFeatureContribution(string Feature, decimal Weight, string Detail);

public sealed record TaskRiskPrediction(
    Guid TaskId,
    string TaskKey,
    decimal ProbabilityOfSlip,
    int DayImpact,
    string Reason,
    IReadOnlyList<RiskFeatureContribution> Features);

public sealed record SprintRiskForecast(
    Guid SprintId,
    decimal CompletionProbability,
    decimal ExpectedPoints,
    DateOnly P50DeliveryDate,
    DateOnly P90DeliveryDate);

/// <param name="TrainingWindowDays">
/// Days of history the prediction drew on; 0 means the model was not fitted to data at all. In the
/// result rather than on the interface because a trained model's window is per-project — one project
/// may have two years of delivery history behind it and another three weeks.
/// </param>
/// <param name="Assumptions">
/// What the reader must know to judge the numbers. Also per-run: a heuristic returns a fixed list,
/// whereas a fitted model would state its actual sample size and cut-off.
/// </param>
public sealed record RiskPredictionResult(
    IReadOnlyList<TaskRiskPrediction> TaskRisks,
    IReadOnlyList<SprintRiskForecast> SprintForecasts,
    int TrainingWindowDays,
    IReadOnlyList<string> Assumptions);

public interface IRiskPredictionModel
{
    /// <summary>Identifies which model produced a run, and is persisted with it.</summary>
    string ModelName { get; }

    Task<RiskPredictionResult> PredictAsync(RiskPredictionInput input, CancellationToken cancellationToken = default);
}

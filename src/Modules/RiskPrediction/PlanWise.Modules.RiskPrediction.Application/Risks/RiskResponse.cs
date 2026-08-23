namespace PlanWise.Modules.RiskPrediction.Application.Risks;

public sealed record TaskRiskResponse(
    Guid Id,
    Guid TaskId,
    string TaskKey,
    decimal ProbabilityOfSlip,
    int DayImpact,
    string Reason,
    DateTime CreatedAtUtc,
    bool Dismissed);

public sealed record FeatureContributionResponse(string Feature, decimal Weight, string Detail);

public sealed record RiskExplanationResponse(
    Guid Id,
    string ModelVersion,
    int TrainingWindowDays,
    IReadOnlyList<FeatureContributionResponse> Drivers,
    IReadOnlyList<string> Assumptions,
    DateTime CreatedAtUtc);

public sealed record SprintForecastResponse(
    Guid SprintId,
    decimal CompletionProbability,
    decimal ExpectedPoints,
    DateOnly P50DeliveryDate,
    DateOnly P90DeliveryDate,
    DateTime CreatedAtUtc);

// The run summary itself — the piece that was previously unreachable after POST /forecasts/run: the
// job's resultLocation pointed at the task-risk list (GET /projects/{id}/risks), which has no notion
// of "the run" as its own addressable thing, unlike CostEstimateRun/ScheduleProposal. TaskCount and
// ForecastedSprintIds let a client tell "did anything come back" apart from drilling into every task
// and sprint individually.
public sealed record LatestForecastResponse(
    Guid RunId,
    string ModelVersion,
    int TrainingWindowDays,
    int TaskCount,
    IReadOnlyList<Guid> ForecastedSprintIds,
    DateTime CreatedAtUtc);

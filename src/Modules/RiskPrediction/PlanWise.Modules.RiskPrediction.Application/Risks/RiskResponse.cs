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

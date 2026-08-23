using System.Text.Json;
using PlanWise.Modules.RiskPrediction.Domain.Risks;

namespace PlanWise.Modules.RiskPrediction.Application.Risks;

internal static class RiskMappings
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string SerializeFeatures(IReadOnlyList<RiskScorer.FeatureContribution> features) =>
        JsonSerializer.Serialize(features, SerializerOptions);

    public static IReadOnlyList<FeatureContributionResponse> DeserializeFeatures(string json) =>
        JsonSerializer.Deserialize<IReadOnlyList<FeatureContributionResponse>>(json, SerializerOptions) ?? [];

    public static TaskRiskResponse ToResponse(TaskRiskAssessment assessment) =>
        new(
            assessment.Id,
            assessment.TaskId,
            assessment.TaskKey,
            assessment.ProbabilityOfSlip,
            assessment.DayImpact,
            assessment.Reason,
            assessment.CreatedAtUtc,
            assessment.Dismissed);

    public static RiskExplanationResponse ToExplanationResponse(TaskRiskAssessment assessment, RiskAssessmentRun run) =>
        new(
            assessment.Id,
            run.ModelVersion,
            run.TrainingWindowDays,
            DeserializeFeatures(assessment.FeatureContributionsJson),
            run.Assumptions,
            assessment.CreatedAtUtc);

    public static SprintForecastResponse ToResponse(SprintForecast forecast) =>
        new(
            forecast.SprintId,
            forecast.CompletionProbability,
            forecast.ExpectedPoints,
            forecast.P50DeliveryDate,
            forecast.P90DeliveryDate,
            forecast.CreatedAtUtc);
}

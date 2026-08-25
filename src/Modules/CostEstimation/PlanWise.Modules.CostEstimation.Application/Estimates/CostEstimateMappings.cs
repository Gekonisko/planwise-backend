using System.Text.Json;
using PlanWise.Modules.CostEstimation.Domain.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Estimates;

internal static class CostEstimateMappings
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string SerializeResult(CostEstimateResult result) =>
        JsonSerializer.Serialize(result, SerializerOptions);

    // Runs persisted before Reductions existed have no "reductions" property in their stored JSON at
    // all — System.Text.Json leaves a missing record-constructor argument as null rather than
    // throwing, so that's normalised to an empty list here rather than at every call site.
    public static CostEstimateResult DeserializeResult(string resultJson)
    {
        CostEstimateResult result = JsonSerializer.Deserialize<CostEstimateResult>(resultJson, SerializerOptions)
            ?? new CostEstimateResult([], [], [], [], [], string.Empty, []);

        return result.Reductions is null ? result with { Reductions = [] } : result;
    }

    public static CostEstimateResponse ToResponse(CostEstimateRun run) =>
        new(run.Id, run.ProjectId, run.JobId, run.ModelName, run.Currency, DeserializeResult(run.ResultJson), run.CreatedAtUtc);

    public static CostEstimateExplanationResponse ToExplanationResponse(CostEstimateRun run)
    {
        CostEstimateResult result = DeserializeResult(run.ResultJson);
        return new CostEstimateExplanationResponse(run.Id, run.ModelName, result.Assumptions, result.Reasoning, run.CreatedAtUtc);
    }

    // Scenarios carry whatever percentile the model chose to label them with (not necessarily an
    // exact 50/90) — this picks whichever scenario's own percentile is closest to the requested one,
    // rather than assuming the model always returns exactly three canonical percentiles.
    public static decimal PickScenarioTotal(IReadOnlyList<CostScenario> scenarios, int targetPercentile) =>
        scenarios.Count == 0
            ? 0m
            : scenarios.OrderBy(scenario => Math.Abs(scenario.Percentile - targetPercentile)).First().Total;
}

using System.Text.Json;
using PlanWise.Modules.CostEstimation.Domain.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Estimates;

internal static class CostEstimateMappings
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string SerializeResult(CostEstimateResult result) =>
        JsonSerializer.Serialize(result, SerializerOptions);

    public static CostEstimateResult DeserializeResult(string resultJson) =>
        JsonSerializer.Deserialize<CostEstimateResult>(resultJson, SerializerOptions)
        ?? new CostEstimateResult([], [], [], [], [], string.Empty);

    public static CostEstimateResponse ToResponse(CostEstimateRun run) =>
        new(run.Id, run.ProjectId, run.JobId, run.ModelName, run.Currency, DeserializeResult(run.ResultJson), run.CreatedAtUtc);

    public static CostEstimateExplanationResponse ToExplanationResponse(CostEstimateRun run)
    {
        CostEstimateResult result = DeserializeResult(run.ResultJson);
        return new CostEstimateExplanationResponse(run.Id, run.ModelName, result.Assumptions, result.Reasoning, run.CreatedAtUtc);
    }
}

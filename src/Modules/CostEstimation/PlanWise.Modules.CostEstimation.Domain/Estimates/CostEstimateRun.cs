using PlanWise.Common.Domain;

namespace PlanWise.Modules.CostEstimation.Domain.Estimates;

// The full structured model output (scenarios, labour table, non-labour lines, priority breakdown,
// assumptions, reasoning) is stored as one JSON blob in ResultJson rather than as relational child
// entities: nothing in it is ever mutated after creation, only read back and reserialized, so a JSON
// column (same approach as OutboxMessage.Content) avoids four extra child tables for no benefit.
public sealed class CostEstimateRun : Entity
{
    private CostEstimateRun()
    {
    }

    private CostEstimateRun(
        Guid projectId,
        Guid jobId,
        string modelName,
        string inputHash,
        string currency,
        string resultJson,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        JobId = jobId;
        ModelName = modelName;
        InputHash = inputHash;
        Currency = currency;
        ResultJson = resultJson;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ProjectId { get; private set; }
    public Guid JobId { get; private set; }
    public string ModelName { get; private set; }
    public string InputHash { get; private set; }
    public string Currency { get; private set; }
    public string ResultJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static CostEstimateRun Create(
        Guid projectId,
        Guid jobId,
        string modelName,
        string inputHash,
        string currency,
        string resultJson,
        DateTime createdAtUtc) =>
        new(projectId, jobId, modelName, inputHash, currency, resultJson, createdAtUtc);
}

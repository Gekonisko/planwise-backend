using PlanWise.Common.Domain;

namespace PlanWise.Modules.RiskPrediction.Domain.Risks;

public sealed class SprintForecast : Entity
{
    private SprintForecast()
    {
    }

    private SprintForecast(
        Guid runId,
        Guid projectId,
        Guid sprintId,
        decimal completionProbability,
        decimal expectedPoints,
        DateOnly p50DeliveryDate,
        DateOnly p90DeliveryDate,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        RunId = runId;
        ProjectId = projectId;
        SprintId = sprintId;
        CompletionProbability = completionProbability;
        ExpectedPoints = expectedPoints;
        P50DeliveryDate = p50DeliveryDate;
        P90DeliveryDate = p90DeliveryDate;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid RunId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid SprintId { get; private set; }
    public decimal CompletionProbability { get; private set; }
    public decimal ExpectedPoints { get; private set; }
    public DateOnly P50DeliveryDate { get; private set; }
    public DateOnly P90DeliveryDate { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static SprintForecast Create(
        Guid runId,
        Guid projectId,
        Guid sprintId,
        decimal completionProbability,
        decimal expectedPoints,
        DateOnly p50DeliveryDate,
        DateOnly p90DeliveryDate,
        DateTime createdAtUtc) =>
        new(runId, projectId, sprintId, completionProbability, expectedPoints, p50DeliveryDate, p90DeliveryDate, createdAtUtc);
}

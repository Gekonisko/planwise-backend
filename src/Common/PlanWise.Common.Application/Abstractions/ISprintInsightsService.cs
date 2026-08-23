namespace PlanWise.Common.Application.Abstractions;

// Owned by Delivery. RiskPrediction needs sprint dates/state to forecast completion but has no
// business reading Delivery's Sprint aggregate directly — narrow, serialization-friendly summary
// only, same shape discipline as IProjectTasksService.
public interface ISprintInsightsService
{
    Task<IReadOnlyList<SprintInsightSummary>> GetSprintsAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<SprintInsightSummary?> GetSprintAsync(Guid sprintId, CancellationToken cancellationToken = default);
}

public sealed record SprintInsightSummary(
    Guid SprintId,
    Guid ProjectId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string State);

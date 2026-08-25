namespace PlanWise.Common.Application.Abstractions;

public interface IProjectTasksService
{
    Task<IReadOnlyList<ScheduleTaskSummary>> GetScheduleTasksAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<ScheduleTaskSummary?> GetScheduleTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<bool> AssignTaskAsync(Guid taskId, Guid assigneeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CostEstimationTaskSummary>> GetCostEstimationTasksAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskInsightSummary>> GetInsightTasksAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<bool> ReorderBacklogAsync(Guid projectId, IReadOnlyList<Guid> orderedTaskIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskSearchSummary>> SearchTasksAsync(Guid projectId, string query, CancellationToken cancellationToken = default);
}

public sealed record ScheduleTaskSummary(
    Guid TaskId,
    Guid ProjectId,
    string Key,
    string Title,
    bool IsDone,
    int? Points,
    DateOnly? DueDate,
    IReadOnlyList<Guid> PredecessorTaskIds,
    Guid? AssigneeId);

public sealed record CostEstimationTaskSummary(
    Guid TaskId,
    string Key,
    string Title,
    string? Description,
    string Priority,
    int? Points,
    bool IsDone,
    DateTime? CompletedAtUtc);

// Shared by RiskPrediction (due-date/dependency/size heuristics) and BacklogPrioritisation
// (value/dependency/complexity scoring) — both read the same underlying task shape, just weight
// different fields, so one summary avoids two near-identical cross-module contracts.
public sealed record TaskInsightSummary(
    Guid TaskId,
    Guid ProjectId,
    string Key,
    string Title,
    string Status,
    string Priority,
    int? Points,
    int? BusinessValue,
    DateOnly? DueDate,
    Guid? AssigneeId,
    Guid? SprintId,
    decimal Rank,
    int SubtaskTotal,
    int SubtaskDone,
    IReadOnlyList<Guid> PredecessorTaskIds,
    int BlocksCount);

public sealed record TaskSearchSummary(Guid TaskId, Guid ProjectId, string Key, string Title);

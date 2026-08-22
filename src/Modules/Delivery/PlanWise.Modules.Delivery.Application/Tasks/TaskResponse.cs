using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks;

public sealed record TaskResponse(
    Guid Id,
    Guid ProjectId,
    Guid? SprintId,
    string Key,
    string Title,
    string? Description,
    ProjectTaskStatus Status,
    TaskPriority Priority,
    int? Points,
    Guid? AssigneeId,
    DateOnly? DueDate,
    decimal Rank,
    int? BusinessValue,
    IReadOnlyList<Guid> LabelIds,
    IReadOnlyList<SubtaskResponse> Subtasks,
    IReadOnlyList<TaskLinkResponse> Links);

public sealed record SubtaskResponse(Guid Id, string Title, bool IsDone);

public sealed record TaskLinkResponse(Guid Id, Guid LinkedTaskId, TaskLinkType Type);

public sealed record CommentResponse(Guid Id, Guid TaskId, Guid AuthorUserId, string Body, DateTime CreatedAtUtc);

public sealed record BoardColumnResponse(ProjectTaskStatus Status, int? WipLimit, int PointTotal, IReadOnlyList<TaskResponse> Tasks);

public sealed record BoardResponse(IReadOnlyList<BoardColumnResponse> Columns);

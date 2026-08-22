using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks;

internal static class TaskMappings
{
    public static TaskResponse ToResponse(ProjectTask task) =>
        new(
            task.Id,
            task.ProjectId,
            task.SprintId,
            task.Key,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.Points,
            task.AssigneeId,
            task.DueDate,
            task.Rank,
            task.BusinessValue,
            task.Labels.Select(label => label.LabelId).ToList(),
            task.Subtasks.Select(ToResponse).ToList(),
            task.Links.Select(ToResponse).ToList());

    public static SubtaskResponse ToResponse(Subtask subtask) => new(subtask.Id, subtask.Title, subtask.IsDone);

    public static TaskLinkResponse ToResponse(TaskLink link) => new(link.Id, link.LinkedTaskId, link.Type);

    public static CommentResponse ToResponse(TaskComment comment) =>
        new(comment.Id, comment.TaskId, comment.AuthorUserId, comment.Body, comment.CreatedAtUtc);
}

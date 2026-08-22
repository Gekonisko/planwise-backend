using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Tasks;

public static class TaskErrors
{
    public static Error ProjectNotFound(Guid projectId) =>
        Error.NotFound("Task.ProjectNotFound", $"The project with identifier {projectId} was not found");

    public static Error NotFound(Guid taskId) =>
        Error.NotFound("Task.NotFound", $"The task with identifier {taskId} was not found");

    public static Error SubtaskNotFound(Guid subtaskId) =>
        Error.NotFound("Task.SubtaskNotFound", $"The subtask with identifier {subtaskId} was not found");

    public static Error LinkNotFound(Guid linkId) =>
        Error.NotFound("Task.LinkNotFound", $"The link with identifier {linkId} was not found");

    public static Error InvalidReorderSet(Guid projectId) =>
        Error.Problem("Task.InvalidReorderSet", $"One or more tasks do not belong to project {projectId}");
}

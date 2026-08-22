using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Sprints;

public static class SprintErrors
{
    public static Error ProjectNotFound(Guid projectId) =>
        Error.NotFound("Sprint.ProjectNotFound", $"The project with identifier {projectId} was not found");

    public static Error NotFound(Guid sprintId) =>
        Error.NotFound("Sprint.NotFound", $"The sprint with identifier {sprintId} was not found");

    public static Error AlreadyActive(Guid projectId) =>
        Error.Conflict("Sprint.AlreadyActive", $"Project {projectId} already has an active sprint");

    public static Error InvalidStateTransition(Guid sprintId, string action) =>
        Error.Problem("Sprint.InvalidStateTransition", $"Cannot {action} sprint {sprintId} from its current state");
}

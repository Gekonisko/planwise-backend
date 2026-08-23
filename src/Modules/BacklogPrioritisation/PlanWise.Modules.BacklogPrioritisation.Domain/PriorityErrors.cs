using PlanWise.Common.Domain;

namespace PlanWise.Modules.BacklogPrioritisation.Domain;

public static class PriorityErrors
{
    public static Error ProjectNotFound(Guid projectId) =>
        Error.NotFound("Priority.ProjectNotFound", $"The project with identifier {projectId} was not found");

    public static Error RunNotFound(Guid runId) =>
        Error.NotFound("Priority.RunNotFound", $"The priority run with identifier {runId} was not found");

    public static Error NoRunForProject(Guid projectId) =>
        Error.NotFound("Priority.NoRun", $"No priority suggestion has been run yet for project {projectId}");

    public static Error InvalidStateTransition(Guid runId) =>
        Error.Problem("Priority.InvalidStateTransition", $"Priority run {runId} has already been applied or dismissed");

    public static Error ReorderFailed(Guid projectId) =>
        Error.Problem("Priority.ReorderFailed", $"Failed to persist the new backlog order for project {projectId}");
}

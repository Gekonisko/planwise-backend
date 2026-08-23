using PlanWise.Common.Domain;

namespace PlanWise.Modules.Scheduling.Domain.Schedule;

public static class ScheduleErrors
{
    public static Error ProjectNotFound(Guid projectId) =>
        Error.NotFound("Schedule.ProjectNotFound", $"The project with identifier {projectId} was not found");

    public static Error TaskNotFound(Guid taskId) =>
        Error.NotFound("Schedule.TaskNotFound", $"The task with identifier {taskId} was not found");

    public static Error DependencyViolation(string reason) =>
        Error.Problem("Schedule.DependencyViolation", reason);

    public static Error ProposalNotFound(Guid proposalId) =>
        Error.NotFound("Schedule.ProposalNotFound", $"The schedule proposal with identifier {proposalId} was not found");

    public static Error NoProposalForProject(Guid projectId) =>
        Error.NotFound("Schedule.NoProposal", $"No optimisation proposal exists yet for project {projectId}");

    public static Error InvalidAssignmentSet(Guid proposalId) =>
        Error.Problem("Schedule.InvalidAssignmentSet", $"One or more assignment ids do not belong to proposal {proposalId}");
}

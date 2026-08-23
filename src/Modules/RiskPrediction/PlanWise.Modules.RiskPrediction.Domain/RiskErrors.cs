using PlanWise.Common.Domain;

namespace PlanWise.Modules.RiskPrediction.Domain;

public static class RiskErrors
{
    public static Error ProjectNotFound(Guid projectId) =>
        Error.NotFound("Risk.ProjectNotFound", $"The project with identifier {projectId} was not found");

    public static Error TaskNotFound(Guid taskId) =>
        Error.NotFound("Risk.TaskNotFound", $"The task with identifier {taskId} was not found");

    public static Error SprintNotFound(Guid sprintId) =>
        Error.NotFound("Risk.SprintNotFound", $"The sprint with identifier {sprintId} was not found");

    public static Error AssessmentNotFound(Guid id) =>
        Error.NotFound("Risk.AssessmentNotFound", $"The risk assessment with identifier {id} was not found");

    public static Error NoAssessmentForTask(Guid taskId) =>
        Error.NotFound("Risk.NoAssessmentForTask", $"No risk assessment has been run yet for task {taskId}");

    public static Error NoForecastForSprint(Guid sprintId) =>
        Error.NotFound("Risk.NoForecastForSprint", $"No forecast has been run yet for sprint {sprintId}");

    public static Error NoRunForProject(Guid projectId) =>
        Error.NotFound("Risk.NoRun", $"No risk forecast has been run yet for project {projectId}");
}

using PlanWise.Common.Domain;

namespace PlanWise.Modules.CostEstimation.Domain;

public static class CostEstimateErrors
{
    public static Error ProjectNotFound(Guid projectId) =>
        Error.NotFound("CostEstimate.ProjectNotFound", $"The project with identifier {projectId} was not found");

    public static Error RunNotFound(Guid runId) =>
        Error.NotFound("CostEstimate.RunNotFound", $"The cost estimate run with identifier {runId} was not found");

    public static Error NoRunForProject(Guid projectId) =>
        Error.NotFound("CostEstimate.NoRun", $"No cost estimate has been run yet for project {projectId}");
}

using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects;

internal static class ProjectMappings
{
    public static ProjectResponse ToResponse(Project project) => new(
        project.Id,
        project.Name,
        project.KeyPrefix,
        project.Process,
        project.Status.ToString(),
        project.OwnerId,
        project.Members.Count,
        project.Labels.Count);
}
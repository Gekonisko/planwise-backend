using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;
using PlanWise.Modules.WorkspaceManagement.Application.Projects;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.GetProjects;

internal sealed class GetProjectsQueryHandler(IProjectRepository projectRepository, IUserContext userContext)
    : IQueryHandler<GetProjectsQuery, IReadOnlyList<ProjectResponse>>
{
    public async Task<Result<IReadOnlyList<ProjectResponse>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId)
        {
            return Result.Failure<IReadOnlyList<ProjectResponse>>(ProjectErrors.NotFound(Guid.Empty));
        }

        IReadOnlyList<Project> projects = await projectRepository.GetForUserAsync(userId, cancellationToken);
        IReadOnlyList<ProjectResponse> responses = projects.Select(ProjectMappings.ToResponse).ToList();
        return Result.Success(responses);
    }
}
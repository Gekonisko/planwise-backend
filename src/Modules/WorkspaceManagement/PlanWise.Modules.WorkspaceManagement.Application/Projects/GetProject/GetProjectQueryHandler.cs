using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Modules.WorkspaceManagement.Application.Projects;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.GetProject;

internal sealed class GetProjectQueryHandler(IProjectRepository projectRepository, IUserContext userContext)
    : IQueryHandler<GetProjectQuery, ProjectResponse>
{
    public async Task<Result<ProjectResponse>> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        Project? project = userContext.UserId is Guid userId
            ? await projectRepository.GetForUserAsync(request.ProjectId, userId, cancellationToken)
            : null;
        return project is null
            ? Result.Failure<ProjectResponse>(ProjectErrors.NotFound(request.ProjectId))
            : Result.Success<ProjectResponse>(ProjectMappings.ToResponse(project));
    }
}
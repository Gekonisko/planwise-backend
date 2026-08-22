using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.WorkspaceManagement.Application.Projects;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.UpdateProject;

internal sealed class UpdateProjectCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork, IUserContext userContext)
    : ICommandHandler<UpdateProjectCommand, ProjectResponse>
{
    public async Task<Result<ProjectResponse>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        Project? project = userContext.UserId is Guid userId
            ? await projectRepository.GetForUserAsync(request.ProjectId, userId, userContext.Email, cancellationToken)
            : null;
        if (project is null)
        {
            return Result.Failure<ProjectResponse>(ProjectErrors.NotFound(request.ProjectId));
        }

        project.Update(request.Name?.Trim(), request.Process, request.ClientName?.Trim());

        if (request.Status is not null && Enum.TryParse(request.Status, ignoreCase: true, out ProjectStatus status))
        {
            project.ChangeStatus(status);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success<ProjectResponse>(ProjectMappings.ToResponse(project));
    }
}
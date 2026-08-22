using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.ArchiveProject;

internal sealed class ArchiveProjectCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork, IUserContext userContext)
    : ICommandHandler<ArchiveProjectCommand>
{
    public async Task<Result> Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
    {
        Project? project = userContext.UserId is Guid userId
            ? await projectRepository.GetForUserAsync(request.ProjectId, userId, userContext.Email, cancellationToken)
            : null;
        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound(request.ProjectId));
        }

        project.Archive();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
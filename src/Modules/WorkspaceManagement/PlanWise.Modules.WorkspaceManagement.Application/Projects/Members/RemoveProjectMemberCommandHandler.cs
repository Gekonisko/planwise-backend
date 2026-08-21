using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

internal sealed class RemoveProjectMemberCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork, IUserContext userContext)
    : ICommandHandler<RemoveProjectMemberCommand>
{
    public async Task<Result> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        Project? project = userContext.UserId is Guid userId
            ? await projectRepository.GetForUserAsync(request.ProjectId, userId, cancellationToken)
            : null;
        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound(request.ProjectId));
        }

        if (!project.Members.Any(member => member.UserId == request.UserId))
        {
            return Result.Failure(ProjectErrors.MemberNotFound(request.UserId));
        }

        project.RemoveMember(request.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
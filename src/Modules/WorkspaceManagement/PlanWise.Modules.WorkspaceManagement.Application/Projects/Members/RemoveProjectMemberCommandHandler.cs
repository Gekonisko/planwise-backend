using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

internal sealed class RemoveProjectMemberCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork, IUserContext userContext)
    : ICommandHandler<RemoveProjectMemberCommand>
{
    public async Task<Result> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        Project? project = userContext.UserId is Guid userId
            ? await projectRepository.GetForUserAsync(request.ProjectId, userId, userContext.Email, cancellationToken)
            : null;
        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound(request.ProjectId));
        }

        if (!project.Members.Any(member => member.Id == request.MemberId))
        {
            return Result.Failure(ProjectErrors.MemberNotFound(request.MemberId));
        }

        project.RemoveMember(request.MemberId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

internal sealed class AddProjectMemberCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork, IUserContext userContext)
    : ICommandHandler<AddProjectMemberCommand, ProjectMemberResponse>
{
    public async Task<Result<ProjectMemberResponse>> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        Project? project = userContext.UserId is Guid userId
            ? await projectRepository.GetForUserAsync(request.ProjectId, userId, userContext.Email, cancellationToken)
            : null;
        if (project is null)
        {
            return Result.Failure<ProjectMemberResponse>(ProjectErrors.NotFound(request.ProjectId));
        }

        string email = request.Email.Trim().ToLowerInvariant();

        if (request.UserId is Guid requestedUserId && project.Members.Any(member => member.UserId == requestedUserId))
        {
            return Result.Failure<ProjectMemberResponse>(ProjectErrors.MemberAlreadyExists(requestedUserId));
        }

        if (project.Members.Any(member => member.Email == email))
        {
            return Result.Failure<ProjectMemberResponse>(ProjectErrors.MemberAlreadyInvited(email));
        }

        ProjectMember member = project.AddMember(
            request.UserId,
            email,
            request.Role,
            request.Capacity,
            request.HourlyRate);
        projectRepository.AddMember(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success<ProjectMemberResponse>(ToResponse(member));
    }

    private static ProjectMemberResponse ToResponse(ProjectMember member) =>
        new(member.Id, member.UserId, member.Email, member.Role, member.Capacity, member.HourlyRate);
}
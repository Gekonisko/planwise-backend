using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

internal sealed class AddProjectMemberCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork, IUserContext userContext)
    : ICommandHandler<AddProjectMemberCommand, ProjectMemberResponse>
{
    public async Task<Result<ProjectMemberResponse>> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        Project? project = userContext.UserId is Guid userId
            ? await projectRepository.GetForUserAsync(request.ProjectId, userId, cancellationToken)
            : null;
        if (project is null)
        {
            return Result.Failure<ProjectMemberResponse>(ProjectErrors.NotFound(request.ProjectId));
        }

        if (project.Members.Any(member => member.UserId == request.UserId))
        {
            return Result.Failure<ProjectMemberResponse>(ProjectErrors.MemberAlreadyExists(request.UserId));
        }

        ProjectMember member = project.AddMember(
            request.UserId,
            request.Email.Trim().ToLowerInvariant(),
            request.Role,
            request.Capacity,
            request.HourlyRate);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success<ProjectMemberResponse>(ToResponse(member));
    }

    private static ProjectMemberResponse ToResponse(ProjectMember member) =>
        new(member.UserId, member.Email, member.Role, member.Capacity, member.HourlyRate);
}
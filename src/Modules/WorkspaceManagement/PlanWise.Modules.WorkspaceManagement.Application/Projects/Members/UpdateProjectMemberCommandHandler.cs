using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

internal sealed class UpdateProjectMemberCommandHandler(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<UpdateProjectMemberCommand, ProjectMemberResponse>
{
    public async Task<Result<ProjectMemberResponse>> Handle(
        UpdateProjectMemberCommand request,
        CancellationToken cancellationToken)
    {
        Project? project = userContext.UserId is Guid userId
            ? await projectRepository.GetForUserAsync(request.ProjectId, userId, userContext.Email, cancellationToken)
            : null;
        if (project is null)
        {
            return Result.Failure<ProjectMemberResponse>(ProjectErrors.NotFound(request.ProjectId));
        }

        ProjectMember? member = project.Members.SingleOrDefault(m => m.Id == request.MemberId);
        if (member is null)
        {
            return Result.Failure<ProjectMemberResponse>(ProjectErrors.MemberNotFound(request.MemberId));
        }

        project.UpdateMember(request.MemberId, request.Role, request.Capacity, request.HourlyRate);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ProjectMemberResponse(
            member.Id,
            member.UserId,
            member.Email,
            member.Role,
            member.Capacity,
            member.HourlyRate));
    }
}

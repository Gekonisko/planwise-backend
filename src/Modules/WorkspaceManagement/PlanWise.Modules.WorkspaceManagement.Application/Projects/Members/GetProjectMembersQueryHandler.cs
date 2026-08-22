using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

internal sealed class GetProjectMembersQueryHandler(IProjectRepository projectRepository, IUserContext userContext)
    : IQueryHandler<GetProjectMembersQuery, IReadOnlyList<ProjectMemberResponse>>
{
    public async Task<Result<IReadOnlyList<ProjectMemberResponse>>> Handle(
        GetProjectMembersQuery request,
        CancellationToken cancellationToken)
    {
        Project? project = userContext.UserId is Guid userId
            ? await projectRepository.GetForUserAsync(request.ProjectId, userId, userContext.Email, cancellationToken)
            : null;
        if (project is null)
        {
            return Result.Failure<IReadOnlyList<ProjectMemberResponse>>(ProjectErrors.NotFound(request.ProjectId));
        }

        IReadOnlyList<ProjectMemberResponse> responses = project.Members
            .Select(member => new ProjectMemberResponse(
                member.Id,
                member.UserId,
                member.Email,
                member.Role,
                member.Capacity,
                member.HourlyRate))
            .ToList();
        return Result.Success(responses);
    }
}

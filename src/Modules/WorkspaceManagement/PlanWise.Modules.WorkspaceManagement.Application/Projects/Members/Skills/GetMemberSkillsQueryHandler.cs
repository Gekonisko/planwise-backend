using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members.Skills;

// Routed as /members/{id}/skills, not nested under /projects/{id} — the member id alone doesn't say
// which project it belongs to, so the project is looked up by member id first and the usual
// owner-or-member access check is applied to it afterward (same predicate IProjectRepository's other
// GetForUserAsync overloads apply in their WHERE clause, just evaluated here in memory instead).
internal sealed class GetMemberSkillsQueryHandler(
    IProjectRepository projectRepository,
    IUserContext userContext)
    : IQueryHandler<GetMemberSkillsQuery, SkillsResponse>
{
    public async Task<Result<SkillsResponse>> Handle(GetMemberSkillsQuery request, CancellationToken cancellationToken)
    {
        Project? project = await projectRepository.GetByMemberIdAsync(request.MemberId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<SkillsResponse>(ProjectErrors.MemberNotFound(request.MemberId));
        }

        if (userContext.UserId is not Guid userId || !HasAccess(project, userId, userContext.Email))
        {
            return Result.Failure<SkillsResponse>(ProjectErrors.MemberNotFound(request.MemberId));
        }

        ProjectMember member = project.Members.Single(m => m.Id == request.MemberId);
        return Result.Success(new SkillsResponse(member.Id, member.Skills));
    }

    internal static bool HasAccess(Project project, Guid userId, string? email) =>
        project.OwnerId == userId ||
        project.Members.Any(member => member.UserId == userId || member.UserId is null && email is not null && member.Email == email);
}

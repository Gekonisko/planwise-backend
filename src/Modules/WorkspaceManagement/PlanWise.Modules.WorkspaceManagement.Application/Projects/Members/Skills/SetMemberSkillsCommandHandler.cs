using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members.Skills;

internal sealed class SetMemberSkillsCommandHandler(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<SetMemberSkillsCommand, SkillsResponse>
{
    public async Task<Result<SkillsResponse>> Handle(SetMemberSkillsCommand request, CancellationToken cancellationToken)
    {
        Project? project = await projectRepository.GetByMemberIdAsync(request.MemberId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<SkillsResponse>(ProjectErrors.MemberNotFound(request.MemberId));
        }

        if (userContext.UserId is not Guid userId || !GetMemberSkillsQueryHandler.HasAccess(project, userId, userContext.Email))
        {
            return Result.Failure<SkillsResponse>(ProjectErrors.MemberNotFound(request.MemberId));
        }

        project.SetMemberSkills(request.MemberId, request.Skills);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        ProjectMember member = project.Members.Single(m => m.Id == request.MemberId);
        return Result.Success(new SkillsResponse(member.Id, member.Skills));
    }
}

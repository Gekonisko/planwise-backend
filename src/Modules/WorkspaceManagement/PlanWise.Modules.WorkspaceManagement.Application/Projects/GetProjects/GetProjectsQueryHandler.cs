using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.WorkspaceManagement.Application.Projects;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.GetProjects;

internal sealed class GetProjectsQueryHandler(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : IQueryHandler<GetProjectsQuery, IReadOnlyList<ProjectResponse>>
{
    public async Task<Result<IReadOnlyList<ProjectResponse>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId)
        {
            return Result.Failure<IReadOnlyList<ProjectResponse>>(ProjectErrors.NotFound(Guid.Empty));
        }

        IReadOnlyList<Project> projects = await projectRepository.GetForUserAsync(userId, userContext.Email, cancellationToken);

        // Links any pending (email-only) invitations on these projects to this account now that we know its user id.
        if (userContext.Email is string email)
        {
            bool claimed = false;

            foreach (Project project in projects)
            {
                claimed |= project.ClaimMembership(email, userId);
            }

            if (claimed)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        IReadOnlyList<ProjectResponse> responses = projects.Select(ProjectMappings.ToResponse).ToList();
        return Result.Success(responses);
    }
}
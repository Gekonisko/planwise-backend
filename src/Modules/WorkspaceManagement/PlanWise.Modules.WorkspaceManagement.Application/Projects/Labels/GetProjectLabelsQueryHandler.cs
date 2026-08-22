using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Labels;

internal sealed class GetProjectLabelsQueryHandler(IProjectRepository projectRepository, IUserContext userContext)
    : IQueryHandler<GetProjectLabelsQuery, IReadOnlyList<LabelResponse>>
{
    public async Task<Result<IReadOnlyList<LabelResponse>>> Handle(GetProjectLabelsQuery request, CancellationToken cancellationToken)
    {
        Project? project = userContext.UserId is Guid userId
            ? await projectRepository.GetForUserAsync(request.ProjectId, userId, userContext.Email, cancellationToken)
            : null;
        if (project is null)
        {
            return Result.Failure<IReadOnlyList<LabelResponse>>(ProjectErrors.NotFound(request.ProjectId));
        }

        IReadOnlyList<LabelResponse> responses = project.Labels
            .Select(label => new LabelResponse(label.Id, label.Name, label.Color))
            .ToList();
        return Result.Success(responses);
    }
}
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Sprints.GetSprints;

internal sealed class GetSprintsQueryHandler(
    ISprintRepository sprintRepository,
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetSprintsQuery, IReadOnlyList<SprintResponse>>
{
    public async Task<Result<IReadOnlyList<SprintResponse>>> Handle(GetSprintsQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<SprintResponse>>(SprintErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<Sprint> sprints = await sprintRepository.GetByProjectAsync(request.ProjectId, cancellationToken);
        IReadOnlyList<ProjectTask> tasks = await taskRepository.GetByProjectAsync(
            request.ProjectId, null, null, null, null, null, cancellationToken);

        IReadOnlyList<SprintResponse> responses = sprints
            .Select(sprint => SprintMappings.ToResponse(sprint, tasks.Where(task => task.SprintId == sprint.Id).ToList()))
            .ToList();
        return Result.Success(responses);
    }
}

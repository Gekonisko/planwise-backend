using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Sprints.GetSprint;

internal sealed class GetSprintQueryHandler(
    ISprintRepository sprintRepository,
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetSprintQuery, SprintResponse>
{
    public async Task<Result<SprintResponse>> Handle(GetSprintQuery request, CancellationToken cancellationToken)
    {
        Sprint? sprint = await sprintRepository.GetAsync(request.SprintId, cancellationToken);
        if (sprint is null)
        {
            return Result.Failure<SprintResponse>(SprintErrors.NotFound(request.SprintId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(sprint.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<SprintResponse>(SprintErrors.NotFound(request.SprintId));
        }

        IReadOnlyList<ProjectTask> sprintTasks = await taskRepository.GetByProjectAsync(
            sprint.ProjectId, sprint.Id, null, null, null, null, cancellationToken);

        return Result.Success(SprintMappings.ToResponse(sprint, sprintTasks));
    }
}

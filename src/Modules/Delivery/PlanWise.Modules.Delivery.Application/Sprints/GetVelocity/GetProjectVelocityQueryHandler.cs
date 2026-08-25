using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Sprints.GetVelocity;

internal sealed class GetProjectVelocityQueryHandler(
    ISprintRepository sprintRepository,
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetProjectVelocityQuery, VelocityResponse>
{
    public async Task<Result<VelocityResponse>> Handle(GetProjectVelocityQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<VelocityResponse>(SprintErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<Sprint> sprints = await sprintRepository.GetByProjectAsync(request.ProjectId, cancellationToken);
        IReadOnlyList<ProjectTask> tasks = await taskRepository.GetByProjectAsync(
            request.ProjectId, null, null, null, null, null, cancellationToken);

        var points = sprints
            .Where(sprint => sprint.State == SprintState.Completed)
            .OrderBy(sprint => sprint.EndDate)
            .Select(sprint =>
            {
                var sprintTasks = tasks.Where(task => task.SprintId == sprint.Id).ToList();
                return new VelocityPoint(
                    sprint.Id,
                    sprint.Name,
                    sprint.EndDate,
                    sprintTasks.Sum(task => task.Points ?? 0),
                    sprintTasks.Where(task => task.Status == ProjectTaskStatus.Done).Sum(task => task.Points ?? 0));
            })
            .ToList();

        return Result.Success(new VelocityResponse(points));
    }
}

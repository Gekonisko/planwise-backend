using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Sprints;
using PlanWise.Modules.Delivery.Application.Tasks;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Overview;

internal sealed class GetOverviewQueryHandler(
    IProjectTaskRepository taskRepository,
    ISprintRepository sprintRepository,
    IProjectAccessService projectAccessService,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : IQueryHandler<GetOverviewQuery, OverviewResponse>
{
    public async Task<Result<OverviewResponse>> Handle(GetOverviewQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<OverviewResponse>(TaskErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<ProjectTask> tasks = await taskRepository.GetByProjectAsync(
            request.ProjectId, null, null, null, null, null, cancellationToken);

        var counts = new TaskStatusCounts(
            tasks.Count(task => task.Status == ProjectTaskStatus.Backlog),
            tasks.Count(task => task.Status == ProjectTaskStatus.Todo),
            tasks.Count(task => task.Status == ProjectTaskStatus.InProgress),
            tasks.Count(task => task.Status == ProjectTaskStatus.Done));

        int totalPoints = tasks.Sum(task => task.Points ?? 0);
        int completedPoints = tasks.Where(task => task.Status == ProjectTaskStatus.Done).Sum(task => task.Points ?? 0);

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        var needsAttention = tasks
            .Where(task => task.Status != ProjectTaskStatus.Done && task.DueDate is not null && task.DueDate < today)
            .OrderBy(task => task.DueDate)
            .Select(TaskMappings.ToResponse)
            .ToList();

        Sprint? activeSprint = await sprintRepository.GetActiveAsync(request.ProjectId, cancellationToken);
        SprintResponse? activeSprintResponse = activeSprint is null
            ? null
            : SprintMappings.ToResponse(activeSprint, tasks.Where(task => task.SprintId == activeSprint.Id).ToList());

        return Result.Success(new OverviewResponse(counts, totalPoints, completedPoints, needsAttention, activeSprintResponse));
    }
}

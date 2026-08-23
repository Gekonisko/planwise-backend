using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Calendar;

internal sealed class GetCalendarQueryHandler(
    IProjectTaskRepository taskRepository,
    ISprintRepository sprintRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetCalendarQuery, CalendarResponse>
{
    public async Task<Result<CalendarResponse>> Handle(GetCalendarQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<CalendarResponse>(TaskErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<ProjectTask> tasks = await taskRepository.GetDueBetweenAsync(request.ProjectId, request.From, request.To, cancellationToken);
        IReadOnlyList<Sprint> sprints = await sprintRepository.GetOverlappingAsync(request.ProjectId, request.From, request.To, cancellationToken);

        var taskResponses = tasks
            .Select(task => new CalendarTaskResponse(task.Id, task.Key, task.Title, task.DueDate!.Value, task.Status))
            .ToList();

        var sprintResponses = sprints
            .Select(sprint => new CalendarSprintResponse(sprint.Id, sprint.Name, sprint.StartDate, sprint.EndDate, sprint.State))
            .ToList();

        return Result.Success(new CalendarResponse(taskResponses, sprintResponses));
    }
}

using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Domain.Milestones;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Schedule.GetSchedule;

internal sealed class GetScheduleQueryHandler(
    IProjectAccessService projectAccessService,
    IProjectTasksService projectTasksService,
    IScheduleItemRepository scheduleItemRepository,
    IMilestoneRepository milestoneRepository,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : IQueryHandler<GetScheduleQuery, ScheduleResponse>
{
    public async Task<Result<ScheduleResponse>> Handle(GetScheduleQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<ScheduleResponse>(ScheduleErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<ScheduleTaskSummary> tasks = await projectTasksService.GetScheduleTasksAsync(request.ProjectId, cancellationToken);
        IReadOnlyList<ScheduleItem> items = await scheduleItemRepository.GetByProjectAsync(request.ProjectId, cancellationToken);
        IReadOnlyList<Milestone> milestones = await milestoneRepository.GetByProjectAsync(request.ProjectId, cancellationToken);

        var overrides = items
            .ToDictionary(item => item.TaskId, item => (item.StartDate, item.EndDate));

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        IReadOnlyDictionary<Guid, ScheduleCalculator.ComputedTaskSchedule> computed =
            ScheduleCalculator.Compute(tasks, overrides, today);

        var taskRows = tasks
            .Select(task =>
            {
                ScheduleCalculator.ComputedTaskSchedule schedule = computed[task.TaskId];
                return new ScheduleTaskRow(
                    task.TaskId,
                    task.Key,
                    task.Title,
                    task.IsDone,
                    schedule.StartDate,
                    schedule.EndDate,
                    schedule.SlackDays,
                    schedule.IsCritical,
                    overrides.ContainsKey(task.TaskId),
                    task.PredecessorTaskIds);
            })
            .ToList();

        var milestoneRows = milestones
            .Select(milestone => new ScheduleMilestoneRow(
                milestone.Id,
                milestone.Name,
                milestone.DueDate,
                milestone.DueDate < today ? "Achieved" : "Upcoming"))
            .ToList();

        var criticalPath = taskRows.Where(row => row.IsCritical).Select(row => row.TaskId).ToList();

        return Result.Success(new ScheduleResponse(taskRows, milestoneRows, criticalPath));
    }
}

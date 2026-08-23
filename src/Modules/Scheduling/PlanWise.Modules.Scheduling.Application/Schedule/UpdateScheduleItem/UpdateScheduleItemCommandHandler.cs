using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Schedule.UpdateScheduleItem;

internal sealed class UpdateScheduleItemCommandHandler(
    IProjectAccessService projectAccessService,
    IProjectTasksService projectTasksService,
    IScheduleItemRepository scheduleItemRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<UpdateScheduleItemCommand, ScheduleTaskRow>
{
    public async Task<Result<ScheduleTaskRow>> Handle(UpdateScheduleItemCommand request, CancellationToken cancellationToken)
    {
        ScheduleTaskSummary? task = await projectTasksService.GetScheduleTaskAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<ScheduleTaskRow>(ScheduleErrors.TaskNotFound(request.TaskId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(task.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<ScheduleTaskRow>(ScheduleErrors.ProjectNotFound(task.ProjectId));
        }

        IReadOnlyList<ScheduleTaskSummary> projectTasks = await projectTasksService.GetScheduleTasksAsync(task.ProjectId, cancellationToken);
        IReadOnlyList<ScheduleItem> existingItems = await scheduleItemRepository.GetByProjectAsync(task.ProjectId, cancellationToken);
        var tasksById = projectTasks.ToDictionary(t => t.TaskId);

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        var currentOverrides = existingItems
            .ToDictionary(item => item.TaskId, item => (item.StartDate, item.EndDate));

        IReadOnlyDictionary<Guid, ScheduleCalculator.ComputedTaskSchedule> currentSchedule =
            ScheduleCalculator.Compute(projectTasks, currentOverrides, today);
        var currentEndDates = currentSchedule.ToDictionary(pair => pair.Key, pair => pair.Value.EndDate);

        string? violation = DependencyValidator.FindViolation(task.TaskId, request.StartDate, tasksById, currentEndDates);
        if (violation is not null)
        {
            return Result.Failure<ScheduleTaskRow>(ScheduleErrors.DependencyViolation(violation));
        }

        ScheduleItem? item = await scheduleItemRepository.GetAsync(task.TaskId, cancellationToken);
        if (item is null)
        {
            item = ScheduleItem.Create(task.TaskId, task.ProjectId, request.StartDate, request.EndDate);
            scheduleItemRepository.Add(item);
        }
        else
        {
            item.Reschedule(request.StartDate, request.EndDate);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        currentOverrides[task.TaskId] = (request.StartDate, request.EndDate);
        IReadOnlyDictionary<Guid, ScheduleCalculator.ComputedTaskSchedule> updatedSchedule =
            ScheduleCalculator.Compute(projectTasks, currentOverrides, today);
        ScheduleCalculator.ComputedTaskSchedule updated = updatedSchedule[task.TaskId];

        return Result.Success(new ScheduleTaskRow(
            task.TaskId,
            task.Key,
            task.Title,
            task.IsDone,
            updated.StartDate,
            updated.EndDate,
            updated.SlackDays,
            updated.IsCritical,
            true,
            task.PredecessorTaskIds));
    }
}

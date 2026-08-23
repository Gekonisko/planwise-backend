using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Schedule.ValidateSchedule;

internal sealed class ValidateScheduleCommandHandler(
    IProjectAccessService projectAccessService,
    IProjectTasksService projectTasksService,
    IScheduleItemRepository scheduleItemRepository,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<ValidateScheduleCommand, ScheduleValidationResponse>
{
    public async Task<Result<ScheduleValidationResponse>> Handle(ValidateScheduleCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<ScheduleValidationResponse>(ScheduleErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<ScheduleTaskSummary> tasks = await projectTasksService.GetScheduleTasksAsync(request.ProjectId, cancellationToken);
        IReadOnlyList<ScheduleItem> existingItems = await scheduleItemRepository.GetByProjectAsync(request.ProjectId, cancellationToken);
        var tasksById = tasks.ToDictionary(task => task.TaskId);

        var proposedOverrides = existingItems
            .ToDictionary(item => item.TaskId, item => (item.StartDate, item.EndDate));

        foreach (ProposedMove move in request.Moves)
        {
            proposedOverrides[move.TaskId] = (move.StartDate, move.EndDate);
        }

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        IReadOnlyDictionary<Guid, ScheduleCalculator.ComputedTaskSchedule> proposedSchedule =
            ScheduleCalculator.Compute(tasks, proposedOverrides, today);
        var proposedEndDates = proposedSchedule.ToDictionary(pair => pair.Key, pair => pair.Value.EndDate);

        var violations = new List<ScheduleViolation>();
        foreach (ProposedMove move in request.Moves)
        {
            string? violation = DependencyValidator.FindViolation(move.TaskId, move.StartDate, tasksById, proposedEndDates);
            if (violation is not null)
            {
                violations.Add(new ScheduleViolation(move.TaskId, violation));
            }
        }

        return Result.Success(new ScheduleValidationResponse(violations));
    }
}

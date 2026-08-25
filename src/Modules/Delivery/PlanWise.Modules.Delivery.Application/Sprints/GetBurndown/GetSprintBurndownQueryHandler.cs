using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Sprints.GetBurndown;

// The ideal line is a straight interpolation from committed points to zero across the sprint's date
// range — the spec's own description ("daily remaining points against the ideal line"). The actual
// line is real, not simulated: it's read from ProjectTask.CompletedAtUtc, stamped the moment a task
// first lands on Done (see ProjectTask.Move). Tasks that were already Done before that tracking
// existed have no real completion date, so they're treated as completed on the sprint's start date —
// a documented best-effort backfill, not a fabricated number for any task completed going forward.
internal sealed class GetSprintBurndownQueryHandler(
    ISprintRepository sprintRepository,
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetSprintBurndownQuery, BurndownResponse>
{
    public async Task<Result<BurndownResponse>> Handle(GetSprintBurndownQuery request, CancellationToken cancellationToken)
    {
        Sprint? sprint = await sprintRepository.GetAsync(request.SprintId, cancellationToken);
        if (sprint is null)
        {
            return Result.Failure<BurndownResponse>(SprintErrors.NotFound(request.SprintId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(sprint.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<BurndownResponse>(SprintErrors.NotFound(request.SprintId));
        }

        IReadOnlyList<ProjectTask> sprintTasks = await taskRepository.GetByProjectAsync(
            sprint.ProjectId, sprint.Id, null, null, null, null, cancellationToken);

        int committedPoints = sprintTasks.Sum(task => task.Points ?? 0);
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        int sprintLengthDays = Math.Max(1, sprint.EndDate.DayNumber - sprint.StartDate.DayNumber);

        var points = new List<BurndownPoint>();
        for (DateOnly day = sprint.StartDate; day <= sprint.EndDate; day = day.AddDays(1))
        {
            int elapsedDays = day.DayNumber - sprint.StartDate.DayNumber;
            decimal idealRemaining = Math.Clamp(
                committedPoints * (1m - (decimal)elapsedDays / sprintLengthDays), 0m, committedPoints);

            decimal? actualRemaining = null;
            if (day <= today)
            {
                DateOnly currentDay = day;
                int completedByDay = sprintTasks
                    .Where(task => task.Status == ProjectTaskStatus.Done)
                    .Where(task => CompletionDay(task, sprint.StartDate) <= currentDay)
                    .Sum(task => task.Points ?? 0);
                actualRemaining = committedPoints - completedByDay;
            }

            points.Add(new BurndownPoint(day, idealRemaining, actualRemaining));
        }

        return Result.Success(new BurndownResponse(sprint.Id, committedPoints, points));
    }

    private static DateOnly CompletionDay(ProjectTask task, DateOnly fallback) =>
        task.CompletedAtUtc is DateTime completedAtUtc ? DateOnly.FromDateTime(completedAtUtc) : fallback;
}

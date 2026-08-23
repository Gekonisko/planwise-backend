using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Calendar;

public sealed record CalendarTaskResponse(Guid Id, string Key, string Title, DateOnly DueDate, ProjectTaskStatus Status);

public sealed record CalendarSprintResponse(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, SprintState State);

public sealed record CalendarResponse(IReadOnlyList<CalendarTaskResponse> Tasks, IReadOnlyList<CalendarSprintResponse> Sprints);

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Scheduling.Application.Schedule.UpdateScheduleItem;

public sealed record UpdateScheduleItemCommand(Guid TaskId, DateOnly StartDate, DateOnly EndDate) : ICommand<ScheduleTaskRow>;

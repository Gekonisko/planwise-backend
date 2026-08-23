using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Scheduling.Application.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Schedule.GetSchedule;

public sealed record GetScheduleQuery(Guid ProjectId) : IQuery<ScheduleResponse>;

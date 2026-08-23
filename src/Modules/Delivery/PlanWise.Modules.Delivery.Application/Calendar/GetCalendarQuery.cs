using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Delivery.Application.Calendar;

public sealed record GetCalendarQuery(Guid ProjectId, DateOnly From, DateOnly To) : IQuery<CalendarResponse>;

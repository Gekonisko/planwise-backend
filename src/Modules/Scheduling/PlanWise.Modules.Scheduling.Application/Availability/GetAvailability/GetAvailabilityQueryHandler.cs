using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Availability.GetAvailability;

internal sealed class GetAvailabilityQueryHandler(
    IProjectAccessService projectAccessService,
    IProjectMembersService projectMembersService,
    IUserContext userContext)
    : IQueryHandler<GetAvailabilityQuery, IReadOnlyList<AvailabilityResponse>>
{
    public async Task<Result<IReadOnlyList<AvailabilityResponse>>> Handle(GetAvailabilityQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<AvailabilityResponse>>(ScheduleErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<ProjectMemberSummary> members = await projectMembersService.GetMembersAsync(request.ProjectId, cancellationToken);
        IReadOnlyList<DateOnly> availableDates = BusinessDaysBetween(request.From, request.To);

        IReadOnlyList<AvailabilityResponse> responses = members
            .Where(member => member.UserId is not null)
            .Select(member => new AvailabilityResponse(member.UserId, member.Email, member.Capacity, availableDates))
            .ToList();

        return Result.Success(responses);
    }

    // No per-day calendar (holidays/leave) is modelled yet — every business day in range is reported
    // as available at the member's flat capacity. Weekends are excluded as the one free simplification
    // that doesn't require new persisted state.
    private static List<DateOnly> BusinessDaysBetween(DateOnly from, DateOnly to)
    {
        var days = new List<DateOnly>();
        for (DateOnly date = from; date <= to; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                days.Add(date);
            }
        }

        return days;
    }
}

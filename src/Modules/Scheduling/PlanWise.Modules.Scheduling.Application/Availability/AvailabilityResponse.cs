namespace PlanWise.Modules.Scheduling.Application.Availability;

public sealed record AvailabilityResponse(Guid? UserId, string Email, decimal Capacity, IReadOnlyList<DateOnly> AvailableDates);

using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Scheduling.Application.Availability;

namespace PlanWise.Modules.Scheduling.Application.Availability.GetAvailability;

public sealed record GetAvailabilityQuery(Guid ProjectId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<AvailabilityResponse>>;

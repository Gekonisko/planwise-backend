namespace PlanWise.Modules.Delivery.Application.Sprints.GetVelocity;

public sealed record VelocityPoint(Guid SprintId, string SprintName, DateOnly EndDate, int CommittedPoints, int CompletedPoints);

public sealed record VelocityResponse(IReadOnlyList<VelocityPoint> Sprints);

namespace PlanWise.Modules.Delivery.Application.Sprints.GetBurndown;

public sealed record BurndownPoint(DateOnly Date, decimal IdealRemainingPoints, decimal? ActualRemainingPoints);

public sealed record BurndownResponse(Guid SprintId, int CommittedPoints, IReadOnlyList<BurndownPoint> Points);

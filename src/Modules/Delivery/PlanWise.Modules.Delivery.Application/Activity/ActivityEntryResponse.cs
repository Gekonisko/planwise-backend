namespace PlanWise.Modules.Delivery.Application.Activity;

public sealed record ActivityEntryResponse(Guid Id, Guid ProjectId, string Description, DateTime OccurredAtUtc);

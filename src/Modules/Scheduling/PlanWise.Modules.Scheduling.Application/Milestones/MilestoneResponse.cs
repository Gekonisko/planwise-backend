namespace PlanWise.Modules.Scheduling.Application.Milestones;

public sealed record MilestoneResponse(Guid Id, Guid ProjectId, string Name, DateOnly DueDate, string Status);

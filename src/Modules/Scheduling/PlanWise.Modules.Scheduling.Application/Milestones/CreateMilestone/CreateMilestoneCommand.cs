using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Scheduling.Application.Milestones;

namespace PlanWise.Modules.Scheduling.Application.Milestones.CreateMilestone;

public sealed record CreateMilestoneCommand(Guid ProjectId, string Name, DateOnly DueDate) : ICommand<MilestoneResponse>;

using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Scheduling.Application.Milestones;

namespace PlanWise.Modules.Scheduling.Application.Milestones.GetMilestones;

public sealed record GetMilestonesQuery(Guid ProjectId) : IQuery<IReadOnlyList<MilestoneResponse>>;

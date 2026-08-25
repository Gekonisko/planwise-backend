using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Delivery.Application.Sprints.GetBurndown;

public sealed record GetSprintBurndownQuery(Guid SprintId) : IQuery<BurndownResponse>;

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Delivery.Application.Overview;

public sealed record GetOverviewQuery(Guid ProjectId) : IQuery<OverviewResponse>;

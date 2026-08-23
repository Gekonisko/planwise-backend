using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Delivery.Application.Activity;

public sealed record GetActivityQuery(Guid ProjectId, int Limit, int Offset) : IQuery<IReadOnlyList<ActivityEntryResponse>>;

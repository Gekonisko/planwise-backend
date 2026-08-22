using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints.GetSprints;

public sealed record GetSprintsQuery(Guid ProjectId) : IQuery<IReadOnlyList<SprintResponse>>;

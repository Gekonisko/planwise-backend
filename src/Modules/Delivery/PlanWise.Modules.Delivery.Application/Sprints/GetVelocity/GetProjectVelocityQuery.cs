using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Delivery.Application.Sprints.GetVelocity;

public sealed record GetProjectVelocityQuery(Guid ProjectId) : IQuery<VelocityResponse>;

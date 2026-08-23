using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Delivery.Application.Workload;

public sealed record GetWorkloadQuery(Guid ProjectId, Guid? SprintId) : IQuery<WorkloadResponse>;

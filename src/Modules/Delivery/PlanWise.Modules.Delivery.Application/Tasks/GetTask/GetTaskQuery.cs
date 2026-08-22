using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.GetTask;

public sealed record GetTaskQuery(Guid TaskId) : IQuery<TaskResponse>;

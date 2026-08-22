using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.GetTasks;

public sealed record GetTasksQuery(
    Guid ProjectId,
    Guid? SprintId,
    string? Status,
    Guid? AssigneeId,
    Guid? Label,
    string? Q) : IQuery<IReadOnlyList<TaskResponse>>;

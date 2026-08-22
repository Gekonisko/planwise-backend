using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.UpdateTask;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    string? Title,
    string? Description,
    string? Priority,
    int? Points,
    Guid? AssigneeId,
    DateOnly? DueDate,
    Guid? SprintId,
    IReadOnlyList<Guid>? LabelIds) : ICommand<TaskResponse>;

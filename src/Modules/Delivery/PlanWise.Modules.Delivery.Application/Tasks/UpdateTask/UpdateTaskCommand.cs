using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.UpdateTask;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    string? Title,
    string? Description,
    string? Priority,
    Optional<int?> Points,
    Optional<Guid?> AssigneeId,
    Optional<DateOnly?> DueDate,
    Optional<Guid?> SprintId,
    IReadOnlyList<Guid>? LabelIds) : ICommand<TaskResponse>;

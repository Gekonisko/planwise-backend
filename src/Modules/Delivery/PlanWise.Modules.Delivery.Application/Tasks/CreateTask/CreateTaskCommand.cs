using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.CreateTask;

public sealed record CreateTaskCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    string Priority,
    int? Points,
    Guid? AssigneeId,
    DateOnly? DueDate,
    int? BusinessValue,
    IReadOnlyList<Guid>? LabelIds) : ICommand<TaskResponse>;

using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Subtasks;

public sealed record UpdateSubtaskCommand(Guid TaskId, Guid SubtaskId, string? Title, bool? IsDone) : ICommand<SubtaskResponse>;

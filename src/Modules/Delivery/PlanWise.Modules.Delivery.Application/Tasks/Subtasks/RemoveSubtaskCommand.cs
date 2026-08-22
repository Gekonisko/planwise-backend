using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Delivery.Application.Tasks.Subtasks;

public sealed record RemoveSubtaskCommand(Guid TaskId, Guid SubtaskId) : ICommand;

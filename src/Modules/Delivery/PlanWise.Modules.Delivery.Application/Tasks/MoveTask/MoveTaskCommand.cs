using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.MoveTask;

public sealed record MoveTaskCommand(Guid TaskId, string Status, int Index) : ICommand<TaskResponse>;

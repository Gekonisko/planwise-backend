using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Delivery.Application.Tasks.DeleteTask;

public sealed record DeleteTaskCommand(Guid TaskId) : ICommand;

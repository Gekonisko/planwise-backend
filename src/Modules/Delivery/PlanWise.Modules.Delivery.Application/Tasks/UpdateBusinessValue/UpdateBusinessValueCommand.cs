using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.UpdateBusinessValue;

public sealed record UpdateBusinessValueCommand(Guid TaskId, int BusinessValue) : ICommand<TaskResponse>;

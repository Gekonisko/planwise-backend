using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Links;

public sealed record AddTaskLinkCommand(Guid TaskId, Guid LinkedTaskId, string Type) : ICommand<TaskLinkResponse>;

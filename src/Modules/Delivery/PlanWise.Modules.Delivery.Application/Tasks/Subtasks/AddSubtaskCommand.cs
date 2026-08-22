using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Subtasks;

public sealed record AddSubtaskCommand(Guid TaskId, string Title) : ICommand<SubtaskResponse>;

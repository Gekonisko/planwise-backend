using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.ReorderTasks;

public sealed record ReorderTasksCommand(Guid ProjectId, IReadOnlyList<Guid> TaskIds) : ICommand<IReadOnlyList<TaskResponse>>;

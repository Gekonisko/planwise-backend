using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Realtime.EventHandlers;

public sealed class ProjectTaskMovedRealtimeHandler(IProjectRealtimeNotifier realtimeNotifier)
    : DomainEventHandler<ProjectTaskMovedDomainEvent>
{
    public override Task Handle(ProjectTaskMovedDomainEvent domainEvent, CancellationToken cancellationToken = default) =>
        realtimeNotifier.TaskMovedAsync(
            domainEvent.ProjectId,
            domainEvent.TaskId,
            domainEvent.Key,
            domainEvent.FromStatus.ToString(),
            domainEvent.ToStatus.ToString(),
            cancellationToken);
}

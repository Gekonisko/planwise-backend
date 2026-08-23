using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Realtime.EventHandlers;

public sealed class ProjectTaskUpdatedRealtimeHandler(IProjectRealtimeNotifier realtimeNotifier)
    : DomainEventHandler<ProjectTaskUpdatedDomainEvent>
{
    public override Task Handle(ProjectTaskUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default) =>
        realtimeNotifier.TaskUpdatedAsync(domainEvent.ProjectId, domainEvent.TaskId, domainEvent.Key, cancellationToken);
}

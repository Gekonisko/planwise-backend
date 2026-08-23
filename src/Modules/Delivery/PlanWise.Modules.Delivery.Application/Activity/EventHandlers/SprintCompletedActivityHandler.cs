using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Domain.Activity;
using PlanWise.Modules.Delivery.Domain.Sprints;

namespace PlanWise.Modules.Delivery.Application.Activity.EventHandlers;

public sealed class SprintCompletedActivityHandler(IActivityLogRepository activityLogRepository)
    : DomainEventHandler<SprintCompletedDomainEvent>
{
    public override Task Handle(SprintCompletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        activityLogRepository.Add(ActivityLogEntry.Create(
            domainEvent.ProjectId,
            $"Sprint \"{domainEvent.Name}\" completed",
            domainEvent.OccurredOnUtc));

        return Task.CompletedTask;
    }
}

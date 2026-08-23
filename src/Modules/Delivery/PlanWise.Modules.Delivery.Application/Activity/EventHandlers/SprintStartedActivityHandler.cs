using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Domain.Activity;
using PlanWise.Modules.Delivery.Domain.Sprints;

namespace PlanWise.Modules.Delivery.Application.Activity.EventHandlers;

public sealed class SprintStartedActivityHandler(IActivityLogRepository activityLogRepository)
    : DomainEventHandler<SprintStartedDomainEvent>
{
    public override Task Handle(SprintStartedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        activityLogRepository.Add(ActivityLogEntry.Create(
            domainEvent.ProjectId,
            $"Sprint \"{domainEvent.Name}\" started",
            domainEvent.OccurredOnUtc));

        return Task.CompletedTask;
    }
}

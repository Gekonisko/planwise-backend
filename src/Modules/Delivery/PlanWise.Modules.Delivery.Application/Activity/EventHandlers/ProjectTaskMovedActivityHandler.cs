using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Domain.Activity;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Activity.EventHandlers;

public sealed class ProjectTaskMovedActivityHandler(IActivityLogRepository activityLogRepository)
    : DomainEventHandler<ProjectTaskMovedDomainEvent>
{
    public override Task Handle(ProjectTaskMovedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        activityLogRepository.Add(ActivityLogEntry.Create(
            domainEvent.ProjectId,
            $"{domainEvent.Key} \"{domainEvent.Title}\" moved from {domainEvent.FromStatus} to {domainEvent.ToStatus}",
            domainEvent.OccurredOnUtc));

        return Task.CompletedTask;
    }
}

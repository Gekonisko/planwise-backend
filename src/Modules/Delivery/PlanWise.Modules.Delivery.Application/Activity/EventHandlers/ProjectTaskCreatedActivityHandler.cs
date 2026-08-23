using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Domain.Activity;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Activity.EventHandlers;

public sealed class ProjectTaskCreatedActivityHandler(IActivityLogRepository activityLogRepository)
    : DomainEventHandler<ProjectTaskCreatedDomainEvent>
{
    public override Task Handle(ProjectTaskCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        activityLogRepository.Add(ActivityLogEntry.Create(
            domainEvent.ProjectId,
            $"{domainEvent.Key} \"{domainEvent.Title}\" created",
            domainEvent.OccurredOnUtc));

        return Task.CompletedTask;
    }
}

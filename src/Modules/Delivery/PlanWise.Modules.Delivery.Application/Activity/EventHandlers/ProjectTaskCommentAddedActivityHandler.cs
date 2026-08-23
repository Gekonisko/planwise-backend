using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Domain.Activity;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Activity.EventHandlers;

public sealed class ProjectTaskCommentAddedActivityHandler(IActivityLogRepository activityLogRepository)
    : DomainEventHandler<ProjectTaskCommentAddedDomainEvent>
{
    public override Task Handle(ProjectTaskCommentAddedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        activityLogRepository.Add(ActivityLogEntry.Create(
            domainEvent.ProjectId,
            $"New comment on {domainEvent.Key}",
            domainEvent.OccurredOnUtc));

        return Task.CompletedTask;
    }
}

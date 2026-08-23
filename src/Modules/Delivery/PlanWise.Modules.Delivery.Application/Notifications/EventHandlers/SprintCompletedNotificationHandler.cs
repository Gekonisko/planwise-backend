using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Domain.Sprints;

namespace PlanWise.Modules.Delivery.Application.Notifications.EventHandlers;

public sealed class SprintCompletedNotificationHandler(
    IProjectMembersService projectMembersService,
    INotificationPublisher notificationPublisher)
    : DomainEventHandler<SprintCompletedDomainEvent>
{
    public override async Task Handle(SprintCompletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProjectMemberSummary> members = await projectMembersService.GetMembersAsync(domainEvent.ProjectId, cancellationToken);

        foreach (ProjectMemberSummary member in members.Where(member => member.UserId is not null))
        {
            await notificationPublisher.PublishAsync(
                member.UserId!.Value,
                domainEvent.ProjectId,
                "SprintEvent",
                $"Sprint \"{domainEvent.Name}\" completed",
                $"/api/v1/projects/{domainEvent.ProjectId}/sprints",
                cancellationToken);
        }
    }
}

using System.Text.RegularExpressions;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Notifications.EventHandlers;

// No real @mention UI/autocomplete exists anywhere in this codebase — this is a best-effort scan of
// the comment body for "@handle" tokens, matched against each project member's email local-part
// (the part before "@"). A member with no matching handle in the body is simply not notified; there
// is no mention storage or validation beyond this one-shot regex scan at comment-creation time.
public sealed partial class ProjectTaskCommentAddedMentionHandler(
    IProjectMembersService projectMembersService,
    INotificationPublisher notificationPublisher)
    : DomainEventHandler<ProjectTaskCommentAddedDomainEvent>
{
    public override async Task Handle(ProjectTaskCommentAddedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        MatchCollection matches = MentionPattern().Matches(domainEvent.Body);
        if (matches.Count == 0)
        {
            return;
        }

        var handles = new HashSet<string>(
            matches.Select(match => match.Groups[1].Value),
            StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<ProjectMemberSummary> members = await projectMembersService.GetMembersAsync(domainEvent.ProjectId, cancellationToken);

        foreach (ProjectMemberSummary member in members)
        {
            if (member.UserId is not Guid memberUserId || memberUserId == domainEvent.AuthorUserId)
            {
                continue;
            }

            string handle = member.Email.Split('@')[0];
            if (!handles.Contains(handle))
            {
                continue;
            }

            await notificationPublisher.PublishAsync(
                memberUserId,
                domainEvent.ProjectId,
                "Mention",
                $"You were mentioned in a comment on {domainEvent.Key}",
                $"/api/v1/tasks/{domainEvent.TaskId}",
                cancellationToken);
        }
    }

    [GeneratedRegex(@"@([A-Za-z0-9_.+-]+)")]
    private static partial Regex MentionPattern();
}

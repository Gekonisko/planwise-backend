using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Modules.Notifications.Domain.Notifications;
using PlanWise.Modules.Notifications.Infrastructure.Database;

namespace PlanWise.Modules.Notifications.Infrastructure.Notifications;

// Implements Common's cross-module INotificationPublisher contract. Self-contained write (persists
// on its own NotificationsDbContext and saves immediately) rather than deferring to the caller's unit
// of work — same pattern as every other cross-module write in this codebase (e.g.
// IProjectTasksService.AssignTaskAsync), since the caller (a different module's DbContext) has no way
// to participate in this module's transaction anyway.
internal sealed class NotificationPublisherService(NotificationsDbContext dbContext, IDateTimeProvider dateTimeProvider)
    : INotificationPublisher
{
    public async Task PublishAsync(
        Guid userId, Guid? projectId, string type, string message, string? link,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(userId, projectId, type, message, link, dateTimeProvider.UtcNow);
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

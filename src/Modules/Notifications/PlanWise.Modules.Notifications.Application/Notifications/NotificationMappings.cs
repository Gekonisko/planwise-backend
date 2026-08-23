using PlanWise.Modules.Notifications.Domain.Notifications;

namespace PlanWise.Modules.Notifications.Application.Notifications;

internal static class NotificationMappings
{
    public static NotificationResponse ToResponse(Notification notification) =>
        new(
            notification.Id,
            notification.ProjectId,
            notification.Type,
            notification.Message,
            notification.Link,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);
}

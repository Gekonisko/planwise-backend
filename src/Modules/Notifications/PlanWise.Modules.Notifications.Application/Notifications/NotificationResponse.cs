namespace PlanWise.Modules.Notifications.Application.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    Guid? ProjectId,
    string Type,
    string Message,
    string? Link,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public sealed record NotificationsResponse(IReadOnlyList<NotificationResponse> Items, int UnreadCount);

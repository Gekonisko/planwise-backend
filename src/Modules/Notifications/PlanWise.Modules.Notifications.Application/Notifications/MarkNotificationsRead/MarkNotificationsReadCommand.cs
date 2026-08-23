using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Notifications.Application.Notifications.MarkNotificationsRead;

public sealed record MarkNotificationsReadCommand(IReadOnlyList<Guid>? NotificationIds) : ICommand;

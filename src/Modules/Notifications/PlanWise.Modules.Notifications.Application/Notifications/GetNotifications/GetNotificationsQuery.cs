using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Notifications.Application.Notifications;

namespace PlanWise.Modules.Notifications.Application.Notifications.GetNotifications;

public sealed record GetNotificationsQuery : IQuery<NotificationsResponse>;

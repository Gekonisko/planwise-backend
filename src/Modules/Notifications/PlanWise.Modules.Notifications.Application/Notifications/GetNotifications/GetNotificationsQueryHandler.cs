using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Notifications.Application.Abstractions.Authentication;
using PlanWise.Modules.Notifications.Domain;
using PlanWise.Modules.Notifications.Domain.Notifications;

namespace PlanWise.Modules.Notifications.Application.Notifications.GetNotifications;

internal sealed class GetNotificationsQueryHandler(
    INotificationRepository notificationRepository,
    IUserContext userContext)
    : IQueryHandler<GetNotificationsQuery, NotificationsResponse>
{
    private const int Limit = 50;

    public async Task<Result<NotificationsResponse>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId)
        {
            return Result.Failure<NotificationsResponse>(NotificationErrors.NotAuthenticated());
        }

        IReadOnlyList<Notification> notifications = await notificationRepository.GetForUserAsync(userId, Limit, cancellationToken);
        int unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);

        return Result.Success(new NotificationsResponse(
            notifications.Select(NotificationMappings.ToResponse).ToList(),
            unreadCount));
    }
}

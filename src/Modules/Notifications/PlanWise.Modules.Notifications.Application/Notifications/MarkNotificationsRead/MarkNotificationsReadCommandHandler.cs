using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Notifications.Application.Abstractions.Authentication;
using PlanWise.Modules.Notifications.Application.Abstractions.Data;
using PlanWise.Modules.Notifications.Domain;
using PlanWise.Modules.Notifications.Domain.Notifications;

namespace PlanWise.Modules.Notifications.Application.Notifications.MarkNotificationsRead;

// No ids -> mark every currently-unread notification for the user as read (the "mark all read" bell
// action); specific ids -> mark only those, silently ignoring any that don't belong to the user
// rather than erroring, since a stale client-side id list shouldn't fail the whole request.
internal sealed class MarkNotificationsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<MarkNotificationsReadCommand>
{
    public async Task<Result> Handle(MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId)
        {
            return Result.Failure(NotificationErrors.NotAuthenticated());
        }

        IReadOnlyList<Notification> notifications = request.NotificationIds is { Count: > 0 } ids
            ? await notificationRepository.GetByIdsForUserAsync(userId, ids, cancellationToken)
            : await notificationRepository.GetUnreadForUserAsync(userId, cancellationToken);

        foreach (Notification notification in notifications)
        {
            notification.MarkRead(dateTimeProvider.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

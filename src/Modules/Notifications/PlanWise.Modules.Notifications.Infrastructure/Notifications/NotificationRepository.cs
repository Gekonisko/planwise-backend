using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.Notifications.Domain.Notifications;
using PlanWise.Modules.Notifications.Infrastructure.Database;

namespace PlanWise.Modules.Notifications.Infrastructure.Notifications;

internal sealed class NotificationRepository(NotificationsDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, int limit, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Notifications
            .Where(notification => notification.UserId == userId && notification.ReadAtUtc == null)
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .Where(notification => notification.UserId == userId && notification.ReadAtUtc == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetByIdsForUserAsync(Guid userId, IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .Where(notification => notification.UserId == userId && ids.Contains(notification.Id))
            .ToListAsync(cancellationToken);

    public void Add(Notification notification) => dbContext.Notifications.Add(notification);
}

namespace PlanWise.Modules.Notifications.Domain.Notifications;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetForUserAsync(Guid userId, int limit, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByIdsForUserAsync(Guid userId, IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);

    void Add(Notification notification);
}

using PlanWise.Common.Domain;

namespace PlanWise.Modules.Notifications.Domain.Notifications;

public sealed class Notification : Entity
{
    private Notification()
    {
    }

    private Notification(
        Guid userId,
        Guid? projectId,
        string type,
        string message,
        string? link,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ProjectId = projectId;
        Type = type;
        Message = message;
        Link = link;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string Type { get; private set; }
    public string Message { get; private set; }
    public string? Link { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public static Notification Create(
        Guid userId,
        Guid? projectId,
        string type,
        string message,
        string? link,
        DateTime createdAtUtc) =>
        new(userId, projectId, type, message, link, createdAtUtc);

    public void MarkRead(DateTime nowUtc) => ReadAtUtc ??= nowUtc;
}

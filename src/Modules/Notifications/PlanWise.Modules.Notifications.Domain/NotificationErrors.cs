using PlanWise.Common.Domain;

namespace PlanWise.Modules.Notifications.Domain;

public static class NotificationErrors
{
    public static Error NotAuthenticated() =>
        Error.Problem("Notification.NotAuthenticated", "The current request is not authenticated");
}

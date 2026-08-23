using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.Notifications.Presentation;

public sealed class NotificationsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        NotificationEndpoints.MapEndpoints(app);
        SearchEndpoints.MapEndpoints(app);
    }
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.Notifications.Application.Notifications.GetNotifications;
using PlanWise.Modules.Notifications.Application.Notifications.MarkNotificationsRead;

namespace PlanWise.Modules.Notifications.Presentation;

public static class NotificationEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/notifications", async (ISender sender) =>
            ToHttp(await sender.Send(new GetNotificationsQuery())));

        group.MapPost("/notifications/read", async (MarkReadRequest? request, ISender sender) =>
        {
            Result result = await sender.Send(new MarkNotificationsReadCommand(request?.NotificationIds));
            return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
        });
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    public sealed record MarkReadRequest(IReadOnlyList<Guid>? NotificationIds);
}

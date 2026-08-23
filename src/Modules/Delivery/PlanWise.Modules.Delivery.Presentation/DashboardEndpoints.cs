using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.Delivery.Application.Activity;
using PlanWise.Modules.Delivery.Application.Calendar;
using PlanWise.Modules.Delivery.Application.Overview;
using PlanWise.Modules.Delivery.Application.Workload;

namespace PlanWise.Modules.Delivery.Presentation;

public static class DashboardEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/projects/{projectId:guid}/overview", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetOverviewQuery(projectId))));

        group.MapGet("/projects/{projectId:guid}/workload", async (Guid projectId, Guid? sprintId, ISender sender) =>
            ToHttp(await sender.Send(new GetWorkloadQuery(projectId, sprintId))));

        group.MapGet("/projects/{projectId:guid}/calendar", async (Guid projectId, DateOnly from, DateOnly to, ISender sender) =>
            ToHttp(await sender.Send(new GetCalendarQuery(projectId, from, to))));

        group.MapGet("/projects/{projectId:guid}/activity", async (Guid projectId, int? limit, int? offset, ISender sender) =>
            ToHttp(await sender.Send(new GetActivityQuery(projectId, limit ?? 20, offset ?? 0))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
}

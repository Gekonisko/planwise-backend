using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.Delivery.Application.Sprints.CompleteSprint;
using PlanWise.Modules.Delivery.Application.Sprints.CreateSprint;
using PlanWise.Modules.Delivery.Application.Sprints.GetBurndown;
using PlanWise.Modules.Delivery.Application.Sprints.GetSprint;
using PlanWise.Modules.Delivery.Application.Sprints.GetSprints;
using PlanWise.Modules.Delivery.Application.Sprints.GetVelocity;
using PlanWise.Modules.Delivery.Application.Sprints.StartSprint;
using PlanWise.Modules.Delivery.Application.Sprints.UpdateSprint;

namespace PlanWise.Modules.Delivery.Presentation;

public static class SprintEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/projects/{projectId:guid}/sprints", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetSprintsQuery(projectId))));

        group.MapPost("/projects/{projectId:guid}/sprints", async (Guid projectId, SprintRequest request, ISender sender) =>
            ToHttp(await sender.Send(new CreateSprintCommand(projectId, request.Name, request.Goal, request.StartDate, request.EndDate))));

        group.MapGet("/sprints/{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetSprintQuery(id))));

        group.MapPatch("/sprints/{id:guid}", async (Guid id, SprintUpdateRequest request, ISender sender) =>
            ToHttp(await sender.Send(new UpdateSprintCommand(id, request.Name, request.Goal, request.StartDate, request.EndDate))));

        group.MapPost("/sprints/{id:guid}/start", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new StartSprintCommand(id))));

        group.MapPost("/sprints/{id:guid}/complete", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new CompleteSprintCommand(id))));

        group.MapGet("/sprints/{id:guid}/burndown", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetSprintBurndownQuery(id))));

        group.MapGet("/projects/{projectId:guid}/velocity", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetProjectVelocityQuery(projectId))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    public sealed record SprintRequest(string Name, string? Goal, DateOnly StartDate, DateOnly EndDate);
    public sealed record SprintUpdateRequest(string? Name, string? Goal, DateOnly? StartDate, DateOnly? EndDate);
}

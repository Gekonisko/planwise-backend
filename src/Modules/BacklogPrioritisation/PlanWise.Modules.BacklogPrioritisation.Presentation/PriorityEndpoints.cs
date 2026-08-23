using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.BacklogPrioritisation.Application.Priorities.ApplyPriorities;
using PlanWise.Modules.BacklogPrioritisation.Application.Priorities.DismissPriorities;
using PlanWise.Modules.BacklogPrioritisation.Application.Priorities.GetPriorities;
using PlanWise.Modules.BacklogPrioritisation.Application.Priorities.GetPriorityExplanation;
using PlanWise.Modules.BacklogPrioritisation.Application.Priorities.RunPriorities;

namespace PlanWise.Modules.BacklogPrioritisation.Presentation;

public static class PriorityEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapPost("/projects/{projectId:guid}/priorities/run", async (Guid projectId, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new RunPrioritiesCommand(projectId));
            return result.IsSuccess
                ? Results.Accepted(value: new JobEnqueuedResponse(result.Value))
                : ApiResults.Problem(result);
        });

        group.MapGet("/projects/{projectId:guid}/priorities", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetPrioritiesQuery(projectId))));

        group.MapPost("/projects/{projectId:guid}/priorities/apply", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new ApplyPrioritiesCommand(projectId))));

        group.MapPost("/projects/{projectId:guid}/priorities/dismiss", async (Guid projectId, DismissPrioritiesRequest? request, ISender sender) =>
        {
            Result result = await sender.Send(new DismissPrioritiesCommand(projectId, request?.Reason));
            return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
        });

        group.MapGet("/priorities/{id:guid}/explanation", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetPriorityExplanationQuery(id))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    public sealed record DismissPrioritiesRequest(string? Reason);
}

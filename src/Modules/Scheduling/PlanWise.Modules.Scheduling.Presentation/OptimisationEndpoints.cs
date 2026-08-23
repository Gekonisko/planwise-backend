using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.Scheduling.Application.Availability;
using PlanWise.Modules.Scheduling.Application.Availability.GetAvailability;
using PlanWise.Modules.Scheduling.Application.Optimisation;
using PlanWise.Modules.Scheduling.Application.Optimisation.ApplyProposal;
using PlanWise.Modules.Scheduling.Application.Optimisation.ApplyProposalPartial;
using PlanWise.Modules.Scheduling.Application.Optimisation.GetLatestProposal;
using PlanWise.Modules.Scheduling.Application.Optimisation.GetProposalExplanation;
using PlanWise.Modules.Scheduling.Application.Optimisation.OptimiseSchedule;

namespace PlanWise.Modules.Scheduling.Presentation;

public static class OptimisationEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapPost("/projects/{projectId:guid}/schedule/optimise", async (Guid projectId, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new OptimiseScheduleCommand(projectId));
            return result.IsSuccess
                ? Results.Accepted(value: new JobEnqueuedResponse(result.Value))
                : ApiResults.Problem(result);
        });

        group.MapGet("/projects/{projectId:guid}/schedule/proposals/latest", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetLatestProposalQuery(projectId))));

        group.MapPost("/schedule/proposals/{id:guid}/apply", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new ApplyProposalCommand(id))));

        group.MapPost("/schedule/proposals/{id:guid}/apply-partial", async (Guid id, ApplyPartialRequest request, ISender sender) =>
            ToHttp(await sender.Send(new ApplyProposalPartialCommand(id, request.AssignmentIds))));

        group.MapGet("/schedule/proposals/{id:guid}/explanation", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetProposalExplanationQuery(id))));

        group.MapGet("/projects/{projectId:guid}/availability", async (Guid projectId, DateOnly from, DateOnly to, ISender sender) =>
            ToHttp(await sender.Send(new GetAvailabilityQuery(projectId, from, to))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    public sealed record ApplyPartialRequest(IReadOnlyList<Guid> AssignmentIds);
}

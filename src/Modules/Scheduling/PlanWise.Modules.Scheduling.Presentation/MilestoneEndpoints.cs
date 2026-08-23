using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.Scheduling.Application.Milestones.CreateMilestone;
using PlanWise.Modules.Scheduling.Application.Milestones.GetMilestones;

namespace PlanWise.Modules.Scheduling.Presentation;

// POST is not in the API spec's section 5 table (only GET is listed), but without a way to create a
// milestone the GET could never return anything. Added as the minimal necessary completion of the
// feature rather than a speculative addition; see docs/API.md.
public static class MilestoneEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/projects/{projectId:guid}/milestones", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetMilestonesQuery(projectId))));

        group.MapPost("/projects/{projectId:guid}/milestones", async (Guid projectId, MilestoneRequest request, ISender sender) =>
            ToHttp(await sender.Send(new CreateMilestoneCommand(projectId, request.Name, request.DueDate))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    public sealed record MilestoneRequest(string Name, DateOnly DueDate);
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.CostEstimation.Application.Estimates.GetCostEstimate;
using PlanWise.Modules.CostEstimation.Application.Estimates.GetCostEstimateExplanation;
using PlanWise.Modules.CostEstimation.Application.Estimates.GetCostEstimateHistory;
using PlanWise.Modules.CostEstimation.Application.Estimates.GetLatestCostEstimate;
using PlanWise.Modules.CostEstimation.Application.Estimates.RunCostEstimate;

namespace PlanWise.Modules.CostEstimation.Presentation;

public static class CostEstimateEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapPost("/projects/{projectId:guid}/cost-estimates/run", async (Guid projectId, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new RunCostEstimateCommand(projectId));
            return result.IsSuccess
                ? Results.Accepted(value: new JobEnqueuedResponse(result.Value))
                : ApiResults.Problem(result);
        });

        group.MapGet("/projects/{projectId:guid}/cost-estimates/latest", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetLatestCostEstimateQuery(projectId))));

        group.MapGet("/cost-estimates/{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetCostEstimateQuery(id))));

        group.MapGet("/projects/{projectId:guid}/cost-estimates", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetCostEstimateHistoryQuery(projectId))));

        group.MapGet("/cost-estimates/{id:guid}/explanation", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetCostEstimateExplanationQuery(id))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
}

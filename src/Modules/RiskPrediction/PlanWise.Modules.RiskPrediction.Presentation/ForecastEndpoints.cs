using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.RiskPrediction.Application.Risks;
using PlanWise.Modules.RiskPrediction.Application.Risks.GetSprintForecast;
using PlanWise.Modules.RiskPrediction.Application.Risks.RunForecast;

namespace PlanWise.Modules.RiskPrediction.Presentation;

public static class ForecastEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapPost("/projects/{projectId:guid}/forecasts/run", async (Guid projectId, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new RunForecastCommand(projectId));
            return result.IsSuccess
                ? Results.Accepted(value: new JobEnqueuedResponse(result.Value))
                : ApiResults.Problem(result);
        });

        group.MapGet("/sprints/{sprintId:guid}/forecast", async (Guid sprintId, ISender sender) =>
            ToHttp(await sender.Send(new GetSprintForecastQuery(sprintId))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
}

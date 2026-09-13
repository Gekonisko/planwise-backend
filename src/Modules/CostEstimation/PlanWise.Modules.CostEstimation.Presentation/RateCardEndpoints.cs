using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.CostEstimation.Application.RateCard.GetRates;

namespace PlanWise.Modules.CostEstimation.Presentation;

public static class RateCardEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        // Project-scoped: the rate card is built from this project's own members, so there is no
        // meaningful project-independent version of it.
        group.MapGet("/projects/{projectId:guid}/rates", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetRatesQuery(projectId))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.CostEstimation.Application.Budget.GetBudget;
using PlanWise.Modules.CostEstimation.Application.Budget.SetBudget;

namespace PlanWise.Modules.CostEstimation.Presentation;

public static class BudgetEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/projects/{projectId:guid}/budget", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetBudgetQuery(projectId))));

        group.MapPut("/projects/{projectId:guid}/budget", async (Guid projectId, BudgetRequest request, ISender sender) =>
            ToHttp(await sender.Send(new SetBudgetCommand(projectId, request.Amount, request.Currency))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    public sealed record BudgetRequest(decimal Amount, string Currency);
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.RiskPrediction.Application.Risks.DismissRisk;
using PlanWise.Modules.RiskPrediction.Application.Risks.GetRiskExplanation;
using PlanWise.Modules.RiskPrediction.Application.Risks.GetRisks;
using PlanWise.Modules.RiskPrediction.Application.Risks.GetTaskRisk;

namespace PlanWise.Modules.RiskPrediction.Presentation;

public static class RiskEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/projects/{projectId:guid}/risks", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetRisksQuery(projectId))));

        group.MapGet("/tasks/{taskId:guid}/risk", async (Guid taskId, ISender sender) =>
            ToHttp(await sender.Send(new GetTaskRiskQuery(taskId))));

        group.MapGet("/risks/{id:guid}/explanation", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetRiskExplanationQuery(id))));

        group.MapPost("/risks/{id:guid}/dismiss", async (Guid id, DismissRiskRequest? request, ISender sender) =>
        {
            Result result = await sender.Send(new DismissRiskCommand(id, request?.Reason));
            return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);
        });
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    public sealed record DismissRiskRequest(string? Reason);
}

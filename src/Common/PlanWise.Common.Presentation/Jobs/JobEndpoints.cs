using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Jobs.GetJob;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Endpoints;
using PlanWise.Common.Presentation.Results;

namespace PlanWise.Common.Presentation.Jobs;

public sealed class JobEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/jobs/{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetJobQuery(id))));
    }

    private static IResult ToHttp(Result<AsyncJobResponse> result) =>
        result.IsSuccess ? Microsoft.AspNetCore.Http.Results.Ok(result.Value) : ApiResults.Problem(result);
}

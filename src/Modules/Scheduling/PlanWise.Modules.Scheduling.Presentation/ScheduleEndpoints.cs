using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.Scheduling.Application.Schedule.GetSchedule;
using PlanWise.Modules.Scheduling.Application.Schedule.UpdateScheduleItem;
using PlanWise.Modules.Scheduling.Application.Schedule.ValidateSchedule;

namespace PlanWise.Modules.Scheduling.Presentation;

public static class ScheduleEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/projects/{projectId:guid}/schedule", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetScheduleQuery(projectId))));

        group.MapPatch("/schedule/items/{taskId:guid}", async (Guid taskId, ScheduleItemRequest request, ISender sender) =>
            ToHttp(await sender.Send(new UpdateScheduleItemCommand(taskId, request.StartDate, request.EndDate))));

        group.MapPost("/projects/{projectId:guid}/schedule/validate", async (Guid projectId, ScheduleValidateRequest request, ISender sender) =>
            ToHttp(await sender.Send(new ValidateScheduleCommand(
                projectId,
                request.Moves.Select(move => new ProposedMove(move.TaskId, move.StartDate, move.EndDate)).ToList()))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    public sealed record ScheduleItemRequest(DateOnly StartDate, DateOnly EndDate);
    public sealed record ScheduleMoveRequest(Guid TaskId, DateOnly StartDate, DateOnly EndDate);
    public sealed record ScheduleValidateRequest(IReadOnlyList<ScheduleMoveRequest> Moves);
}

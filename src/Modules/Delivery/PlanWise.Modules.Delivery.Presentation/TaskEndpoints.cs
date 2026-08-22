using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.Delivery.Application.Tasks.Comments;
using PlanWise.Modules.Delivery.Application.Tasks.CreateTask;
using PlanWise.Modules.Delivery.Application.Tasks.DeleteTask;
using PlanWise.Modules.Delivery.Application.Tasks.GetBoard;
using PlanWise.Modules.Delivery.Application.Tasks.GetTask;
using PlanWise.Modules.Delivery.Application.Tasks.GetTasks;
using PlanWise.Modules.Delivery.Application.Tasks.Links;
using PlanWise.Modules.Delivery.Application.Tasks.MoveTask;
using PlanWise.Modules.Delivery.Application.Tasks.ReorderTasks;
using PlanWise.Modules.Delivery.Application.Tasks.Subtasks;
using PlanWise.Modules.Delivery.Application.Tasks.UpdateBusinessValue;
using PlanWise.Modules.Delivery.Application.Tasks.UpdateTask;

namespace PlanWise.Modules.Delivery.Presentation;

public static class TaskEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();

        group.MapGet("/projects/{projectId:guid}/tasks", async (
            Guid projectId, Guid? sprintId, string? status, Guid? assigneeId, Guid? label, string? q, ISender sender) =>
            ToHttp(await sender.Send(new GetTasksQuery(projectId, sprintId, status, assigneeId, label, q))));

        group.MapGet("/projects/{projectId:guid}/board", async (Guid projectId, ISender sender) =>
            ToHttp(await sender.Send(new GetBoardQuery(projectId))));

        group.MapPost("/projects/{projectId:guid}/tasks", async (Guid projectId, TaskRequest request, ISender sender) =>
            ToHttp(await sender.Send(new CreateTaskCommand(
                projectId, request.Title, request.Description, request.Priority, request.Points,
                request.AssigneeId, request.DueDate, request.BusinessValue, request.LabelIds))));

        group.MapPost("/projects/{projectId:guid}/tasks/reorder", async (Guid projectId, ReorderRequest request, ISender sender) =>
            ToHttp(await sender.Send(new ReorderTasksCommand(projectId, request.TaskIds))));

        group.MapGet("/tasks/{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetTaskQuery(id))));

        group.MapPatch("/tasks/{id:guid}", async (Guid id, TaskUpdateRequest request, ISender sender) =>
            ToHttp(await sender.Send(new UpdateTaskCommand(
                id, request.Title, request.Description, request.Priority, request.Points,
                request.AssigneeId, request.DueDate, request.SprintId, request.LabelIds))));

        group.MapDelete("/tasks/{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new DeleteTaskCommand(id))));

        group.MapPost("/tasks/{id:guid}/move", async (Guid id, MoveRequest request, ISender sender) =>
            ToHttp(await sender.Send(new MoveTaskCommand(id, request.Status, request.Index))));

        group.MapPut("/tasks/{id:guid}/business-value", async (Guid id, BusinessValueRequest request, ISender sender) =>
            ToHttp(await sender.Send(new UpdateBusinessValueCommand(id, request.BusinessValue))));

        group.MapPost("/tasks/{id:guid}/subtasks", async (Guid id, SubtaskRequest request, ISender sender) =>
            ToHttp(await sender.Send(new AddSubtaskCommand(id, request.Title))));

        group.MapPatch("/tasks/{id:guid}/subtasks/{subId:guid}", async (Guid id, Guid subId, SubtaskUpdateRequest request, ISender sender) =>
            ToHttp(await sender.Send(new UpdateSubtaskCommand(id, subId, request.Title, request.IsDone))));

        group.MapDelete("/tasks/{id:guid}/subtasks/{subId:guid}", async (Guid id, Guid subId, ISender sender) =>
            ToHttp(await sender.Send(new RemoveSubtaskCommand(id, subId))));

        group.MapGet("/tasks/{id:guid}/comments", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetCommentsQuery(id))));

        group.MapPost("/tasks/{id:guid}/comments", async (Guid id, CommentRequest request, ISender sender) =>
            ToHttp(await sender.Send(new AddCommentCommand(id, request.Body))));

        group.MapPost("/tasks/{id:guid}/links", async (Guid id, LinkRequest request, ISender sender) =>
            ToHttp(await sender.Send(new AddTaskLinkCommand(id, request.LinkedTaskId, request.Type))));

        group.MapDelete("/tasks/{id:guid}/links/{linkId:guid}", async (Guid id, Guid linkId, ISender sender) =>
            ToHttp(await sender.Send(new RemoveTaskLinkCommand(id, linkId))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    private static IResult ToHttp(Result result) =>
        result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);

    public sealed record TaskRequest(
        string Title,
        string? Description,
        string Priority,
        int? Points,
        Guid? AssigneeId,
        DateOnly? DueDate,
        int? BusinessValue,
        IReadOnlyList<Guid>? LabelIds);

    public sealed record TaskUpdateRequest(
        string? Title,
        string? Description,
        string? Priority,
        int? Points,
        Guid? AssigneeId,
        DateOnly? DueDate,
        Guid? SprintId,
        IReadOnlyList<Guid>? LabelIds);

    public sealed record ReorderRequest(IReadOnlyList<Guid> TaskIds);

    public sealed record MoveRequest(string Status, int Index);

    public sealed record BusinessValueRequest(int BusinessValue);

    public sealed record SubtaskRequest(string Title);

    public sealed record SubtaskUpdateRequest(string? Title, bool? IsDone);

    public sealed record CommentRequest(string Body);

    public sealed record LinkRequest(Guid LinkedTaskId, string Type);
}

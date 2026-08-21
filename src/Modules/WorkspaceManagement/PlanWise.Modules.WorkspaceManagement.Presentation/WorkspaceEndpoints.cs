using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Modules.WorkspaceManagement.Application.Projects;
using PlanWise.Modules.WorkspaceManagement.Application.Projects.ArchiveProject;
using PlanWise.Modules.WorkspaceManagement.Application.Projects.CreateProject;
using PlanWise.Modules.WorkspaceManagement.Application.Projects.GetProject;
using PlanWise.Modules.WorkspaceManagement.Application.Projects.GetProjects;
using PlanWise.Modules.WorkspaceManagement.Application.Projects.Labels;
using PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;
using PlanWise.Modules.WorkspaceManagement.Application.Projects.UpdateProject;
using PlanWise.Common.Domain;

namespace PlanWise.Modules.WorkspaceManagement.Presentation;

public static class WorkspaceEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();
        group.MapGet("/projects", async (ISender sender) => ToHttp(await sender.Send(new GetProjectsQuery())));
        group.MapPost("/projects", async (ProjectRequest request, ISender sender) =>
            ToHttp(await sender.Send(new CreateProjectCommand(request.Name, request.KeyPrefix, request.Process))));
        group.MapGet("/projects/{id:guid}", async (Guid id, ISender sender) => ToHttp(await sender.Send(new GetProjectQuery(id))));
        group.MapPatch("/projects/{id:guid}", async (Guid id, ProjectUpdateRequest request, ISender sender) =>
            ToHttp(await sender.Send(new UpdateProjectCommand(id, request.Name, request.Process))));
        group.MapDelete("/projects/{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new ArchiveProjectCommand(id))));
        group.MapGet("/projects/{id:guid}/labels", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetProjectLabelsQuery(id))));
        group.MapPost("/projects/{id:guid}/members", async (Guid id, MemberRequest request, ISender sender) =>
            ToHttp(await sender.Send(new AddProjectMemberCommand(id, request.UserId, request.Email, request.Role, request.Capacity, request.HourlyRate))));
        group.MapDelete("/projects/{id:guid}/members/{userId:guid}", async (Guid id, Guid userId, ISender sender) =>
            ToHttp(await sender.Send(new RemoveProjectMemberCommand(id, userId))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error);

    private static IResult ToHttp(Result result) =>
        result.IsSuccess ? Results.NoContent() : Problem(result.Error!);

    private static IResult Problem(Error error) => error.Type switch
    {
        ErrorType.NotFound => Results.NotFound(new { error.Code, error.Description }),
        ErrorType.Conflict => Results.Conflict(new { error.Code, error.Description }),
        _ => Results.BadRequest(new { error.Code, error.Description })
    };

    public sealed record ProjectRequest(string Name, string KeyPrefix, string Process);
    public sealed record ProjectUpdateRequest(string Name, string Process);
    public sealed record MemberRequest(Guid UserId, string Email, string Role, decimal Capacity, decimal HourlyRate);
}
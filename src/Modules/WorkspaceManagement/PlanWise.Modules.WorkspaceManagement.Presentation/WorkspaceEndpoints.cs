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
using PlanWise.Common.Presentation.Results;

namespace PlanWise.Modules.WorkspaceManagement.Presentation;

public static class WorkspaceEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1").RequireAuthorization();
        group.MapGet("/projects", async (ISender sender) => ToHttp(await sender.Send(new GetProjectsQuery())));
        group.MapPost("/projects", async (ProjectRequest request, ISender sender) =>
            ToHttp(await sender.Send(new CreateProjectCommand(request.Name, request.KeyPrefix, request.Process, request.ClientName))));
        group.MapGet("/projects/{id:guid}", async (Guid id, ISender sender) => ToHttp(await sender.Send(new GetProjectQuery(id))));
        group.MapPatch("/projects/{id:guid}", async (Guid id, ProjectUpdateRequest request, ISender sender) =>
            ToHttp(await sender.Send(new UpdateProjectCommand(id, request.Name, request.Process, request.ClientName, request.Status))));
        group.MapDelete("/projects/{id:guid}", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new ArchiveProjectCommand(id))));
        group.MapGet("/projects/{id:guid}/labels", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetProjectLabelsQuery(id))));
        group.MapGet("/projects/{id:guid}/members", async (Guid id, ISender sender) =>
            ToHttp(await sender.Send(new GetProjectMembersQuery(id))));
        group.MapPost("/projects/{id:guid}/members", async (Guid id, MemberRequest request, ISender sender) =>
            ToHttp(await sender.Send(new AddProjectMemberCommand(id, request.UserId, request.Email, request.Role, request.Capacity, request.HourlyRate))));
        group.MapPatch("/projects/{id:guid}/members/{memberId:guid}", async (Guid id, Guid memberId, MemberUpdateRequest request, ISender sender) =>
            ToHttp(await sender.Send(new UpdateProjectMemberCommand(id, memberId, request.Role, request.Capacity, request.HourlyRate))));
        group.MapDelete("/projects/{id:guid}/members/{memberId:guid}", async (Guid id, Guid memberId, ISender sender) =>
            ToHttp(await sender.Send(new RemoveProjectMemberCommand(id, memberId))));
    }

    private static IResult ToHttp<T>(Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    private static IResult ToHttp(Result result) =>
        result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result);

    public sealed record ProjectRequest(string Name, string KeyPrefix, string Process, string? ClientName = null);
    public sealed record ProjectUpdateRequest(string? Name, string? Process, string? ClientName = null, string? Status = null);
    public sealed record MemberRequest(Guid? UserId, string Email, string Role, decimal Capacity, decimal HourlyRate);
    public sealed record MemberUpdateRequest(string Role, decimal Capacity, decimal HourlyRate);
}
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Common.Presentation.Hubs;

// WS /hubs/project/{id}: task moved, task updated, job finished — keeps two managers on one board in
// sync. Connections don't ride the normal request pipeline (no controller-style [Authorize] filter
// context), so access is checked here directly against Context.User rather than through IUserContext
// — that reads via IHttpContextAccessor, which isn't reliably populated for the lifetime of a
// long-running WebSocket connection.
public sealed class ProjectHub(IProjectAccessService projectAccessService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        HttpContext? httpContext = Context.GetHttpContext();
        string? projectIdValue = httpContext?.Request.RouteValues["id"] as string;
        string? userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        string? email = Context.User?.FindFirstValue(ClaimTypes.Email);

        if (!Guid.TryParse(projectIdValue, out Guid projectId) ||
            !Guid.TryParse(userIdValue, out Guid userId) ||
            !await projectAccessService.HasAccessAsync(projectId, userId, email, Context.ConnectionAborted))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(projectId), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public static string GroupName(Guid projectId) => $"project:{projectId}";
}

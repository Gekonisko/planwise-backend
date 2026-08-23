using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Common.Presentation.Hubs;

public sealed class ProjectHubEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapHub<ProjectHub>("/hubs/project/{id:guid}").RequireAuthorization();
}

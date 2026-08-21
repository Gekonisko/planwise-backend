using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.WorkspaceManagement.Presentation;

public sealed class WorkspaceManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => WorkspaceEndpoints.MapEndpoints(app);
}

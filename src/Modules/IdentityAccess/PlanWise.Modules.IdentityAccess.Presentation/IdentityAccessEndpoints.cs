using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Presentation.Endpoints;
using PlanWise.Modules.IdentityAccess.Presentation.Authentication;
using PlanWise.Modules.IdentityAccess.Presentation.Users;

namespace PlanWise.Modules.IdentityAccess.Presentation;

public sealed class IdentityAccessEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        AuthEndpoints.MapEndpoints(app);
        PreferencesEndpoints.MapEndpoints(app);
    }
}

using Microsoft.AspNetCore.Routing;

namespace PlanWise.Modules.IdentityAccess.Presentation.Users;

public static class UserEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        CreateUser.MapEndpoint(app);
        GetUser.MapEndpoint(app);
    }
}

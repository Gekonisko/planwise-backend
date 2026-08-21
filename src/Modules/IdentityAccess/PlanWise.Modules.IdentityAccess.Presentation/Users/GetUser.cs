using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Users.GetUser;
using PlanWise.Modules.IdentityAccess.Presentation.ApiResults;

namespace PlanWise.Modules.IdentityAccess.Presentation.Users;

internal static class GetUser
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{id}", async (Guid id, ISender sender) =>
        {
            Result<UserResponse> result = await sender.Send(new GetUserQuery(id));

            return result.Match(Results.Ok, ApiResults.ApiResults.Problem);
        });
    }
}

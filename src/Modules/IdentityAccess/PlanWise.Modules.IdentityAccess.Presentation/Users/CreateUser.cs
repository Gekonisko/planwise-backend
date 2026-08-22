using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Users.CreateUser;
using PlanWise.Common.Presentation.Results;

namespace PlanWise.Modules.IdentityAccess.Presentation.Users;

internal static class CreateUser
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new CreateUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.Password));

            return result.Match(Results.Ok, ApiResults.Problem);
        });
    }

    internal sealed class Request
    {
        public string Email { get; init; }
        public string FirstName { get; init; }
        public string LastName { get; init; }
        public string Password { get; init; }
    }
}

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Domain;
using PlanWise.Common.Presentation.Results;
using PlanWise.Modules.IdentityAccess.Application.Users.Preferences;
using PlanWise.Modules.IdentityAccess.Application.Users.Preferences.GetPreferences;
using PlanWise.Modules.IdentityAccess.Application.Users.Preferences.SetPreferences;

namespace PlanWise.Modules.IdentityAccess.Presentation.Users;

public static class PreferencesEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/me").RequireAuthorization();

        group.MapGet("/preferences", async (ISender sender) =>
            ToHttp(await sender.Send(new GetPreferencesQuery())));

        group.MapPut("/preferences", async (SetPreferencesRequest request, ISender sender) =>
            ToHttp(await sender.Send(new SetPreferencesCommand(request.BoardGrouping, request.WipDisplay, request.DefaultProjectId))));
    }

    private static IResult ToHttp(Result<PreferencesResponse> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result);

    public sealed record SetPreferencesRequest(string BoardGrouping, bool WipDisplay, Guid? DefaultProjectId);
}

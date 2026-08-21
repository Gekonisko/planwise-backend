using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Users.ForgotPassword;
using PlanWise.Modules.IdentityAccess.Application.Users.GetCurrentUser;
using PlanWise.Modules.IdentityAccess.Application.Users.Login;
using PlanWise.Modules.IdentityAccess.Application.Users.Logout;
using PlanWise.Modules.IdentityAccess.Application.Users.Refresh;
using PlanWise.Modules.IdentityAccess.Application.Users.Register;
using PlanWise.Modules.IdentityAccess.Application.Users.ResetPassword;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;
using PlanWise.Modules.IdentityAccess.Domain.Users;
using PlanWise.Modules.IdentityAccess.Presentation.ApiResults;

namespace PlanWise.Modules.IdentityAccess.Presentation.Authentication;

public static class AuthEndpoints
{
    private const string RefreshTokenCookie = "planwise_refresh_token";

    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/auth");

        group.MapPost("/register", async (RegisterRequest request, ISender sender, HttpResponse response) =>
        {
            Result<AuthenticationResponse> result = await sender.Send(new RegisterUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.Password));

            return WriteAuthenticationResult(result, response);
        });

        group.MapPost("/login", async (LoginRequest request, ISender sender, HttpResponse response) =>
        {
            Result<AuthenticationResponse> result = await sender.Send(new LoginCommand(
                request.Email,
                request.Password));

            return WriteAuthenticationResult(result, response);
        });

        group.MapPost("/refresh", async (HttpRequest request, ISender sender, HttpResponse response) =>
        {
            string? token = request.Cookies[RefreshTokenCookie];

            if (string.IsNullOrWhiteSpace(token))
            {
                return ApiResults.ApiResults.Problem(Result.Failure(UserErrors.InvalidRefreshToken()));
            }

            Result<AuthenticationResponse> result = await sender.Send(new RefreshTokenCommand(token));
            return WriteAuthenticationResult(result, response);
        });

        group.MapPost("/logout", async (HttpRequest request, ISender sender, HttpResponse response) =>
        {
            string? token = request.Cookies[RefreshTokenCookie];
            response.Cookies.Delete(RefreshTokenCookie, GetCookieOptions());

            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.NoContent();
            }

            Result result = await sender.Send(new LogoutCommand(token));
            return result.Match(() => Results.NoContent(), ApiResults.ApiResults.Problem);
        });

        group.MapPost("/password/forgot", async (ForgotPasswordRequest request, ISender sender) =>
        {
            Result result = await sender.Send(new ForgotPasswordCommand(request.Email));
            return result.Match(() => Results.Accepted(), ApiResults.ApiResults.Problem);
        });

        group.MapPost("/password/reset", async (ResetPasswordRequest request, ISender sender) =>
        {
            Result result = await sender.Send(new ResetPasswordCommand(request.Token, request.Password));
            return result.Match(() => Results.NoContent(), ApiResults.ApiResults.Problem);
        });

        group.MapGet("/me", async (ISender sender) =>
        {
            Result<CurrentUserResponse> result = await sender.Send(new GetCurrentUserQuery());
            return result.Match(Results.Ok, ApiResults.ApiResults.Problem);
        }).RequireAuthorization();
    }

    private static IResult WriteAuthenticationResult(
        Result<AuthenticationResponse> result,
        HttpResponse response)
    {
        if (result.IsFailure)
        {
            return ApiResults.ApiResults.Problem(result);
        }

        AuthenticationResponse value = result.Value;
        response.Cookies.Append(
            RefreshTokenCookie,
            value.RefreshToken,
            GetCookieOptions(value.RefreshTokenExpiresAtUtc));

        return Results.Ok(new
        {
            value.UserId,
            value.Email,
            value.FirstName,
            value.LastName,
            value.Roles,
            value.AccessToken,
            value.AccessTokenExpiresAtUtc
        });
    }

    private static CookieOptions GetCookieOptions(DateTime? expiresAtUtc = null) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth",
        Expires = expiresAtUtc
    };

    public sealed record RegisterRequest(string Email, string FirstName, string LastName, string Password);

    public sealed record LoginRequest(string Email, string Password);

    public sealed record ForgotPasswordRequest(string Email);

    public sealed record ResetPasswordRequest(string Token, string Password);
}
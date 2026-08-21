namespace PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;

public sealed record AuthenticationResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string[] Roles,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
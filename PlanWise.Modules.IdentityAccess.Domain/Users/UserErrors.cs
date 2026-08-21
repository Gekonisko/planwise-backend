using PlanWise.Modules.IdentityAccess.Domain.Abstractions;

namespace PlanWise.Modules.IdentityAccess.Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) =>
        Error.NotFound("User.NotFound", $"The user with the identifier {userId} was not found");

    public static Error EmailNotUnique(string userEmail) =>
        Error.Conflict("User.Conflict", $"The user with the email {userEmail} already exist");

    public static Error InvalidCredentials() =>
        Error.Problem("Auth.InvalidCredentials", "The email or password is incorrect");

    public static Error InvalidRefreshToken() =>
        Error.Problem("Auth.InvalidRefreshToken", "The refresh token is invalid or expired");

    public static Error InvalidPasswordResetToken() =>
        Error.Problem("Auth.InvalidPasswordResetToken", "The password reset token is invalid or expired");

    public static Error NotAuthenticated() =>
        Error.Unauthorized("Auth.NotAuthenticated", "Authentication is required");
}

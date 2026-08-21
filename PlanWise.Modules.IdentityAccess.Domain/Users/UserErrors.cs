using PlanWise.Modules.IdentityAccess.Domain.Abstractions;

namespace PlanWise.Modules.IdentityAccess.Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) =>
        Error.NotFound("User.NotFound", $"The user with the identifier {userId} was not found");

    public static Error EmailNotUnique(string userEmail) =>
        Error.Conflict("User.Conflict", $"The user with the email {userEmail} already exist");
}

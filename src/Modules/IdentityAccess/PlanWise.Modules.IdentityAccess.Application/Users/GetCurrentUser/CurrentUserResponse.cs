namespace PlanWise.Modules.IdentityAccess.Application.Users.GetCurrentUser;

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string[] Roles,
    string[] Permissions);
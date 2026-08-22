using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.IdentityAccess.Domain.Roles;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Application.Users.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler(
    IUserContext userContext,
    IUserRepository userRepository)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    public async Task<Result<CurrentUserResponse>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId)
        {
            return Result.Failure<CurrentUserResponse>(UserErrors.NotAuthenticated());
        }

        User? user = await userRepository.GetAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<CurrentUserResponse>(UserErrors.NotFound(userId));
        }

        string[] roleNames = user.Roles.Select(role => role.Name).ToArray();

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            roleNames,
            RolePermissions.For(roleNames));
    }
}
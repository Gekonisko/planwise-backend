using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;
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

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles.Select(role => role.Name).ToArray());
    }
}
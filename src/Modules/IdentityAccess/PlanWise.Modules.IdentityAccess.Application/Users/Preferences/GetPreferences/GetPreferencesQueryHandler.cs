using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Preferences.GetPreferences;

internal sealed class GetPreferencesQueryHandler(
    IUserContext userContext,
    IUserRepository userRepository)
    : IQueryHandler<GetPreferencesQuery, PreferencesResponse>
{
    private const string DefaultBoardGrouping = "status";

    public async Task<Result<PreferencesResponse>> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId)
        {
            return Result.Failure<PreferencesResponse>(UserErrors.NotAuthenticated());
        }

        User? user = await userRepository.GetAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<PreferencesResponse>(UserErrors.NotFound(userId));
        }

        return Result.Success(new PreferencesResponse(
            user.BoardGrouping ?? DefaultBoardGrouping,
            user.WipDisplay ?? true,
            user.DefaultProjectId));
    }
}

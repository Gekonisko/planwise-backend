using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Data;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Preferences.SetPreferences;

internal sealed class SetPreferencesCommandHandler(
    IUserContext userContext,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetPreferencesCommand, PreferencesResponse>
{
    public async Task<Result<PreferencesResponse>> Handle(SetPreferencesCommand request, CancellationToken cancellationToken)
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

        user.SetPreferences(request.BoardGrouping, request.WipDisplay, request.DefaultProjectId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PreferencesResponse(request.BoardGrouping, request.WipDisplay, request.DefaultProjectId));
    }
}

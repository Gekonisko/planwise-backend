using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    RefreshTokenData CreateRefreshToken();

    RefreshTokenData CreatePasswordResetToken();

    string HashToken(string token);
}
using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Data;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Application.Services;

public sealed class AuthenticationService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService)
{
    public async Task<Result<AuthenticationResponse>> CreateSessionAsync(
        User user,
        CancellationToken cancellationToken)
    {
        AccessToken accessToken = tokenService.CreateAccessToken(user);
        RefreshTokenData refreshToken = tokenService.CreateRefreshToken();

        refreshTokenRepository.Add(RefreshToken.Create(
            user.Id,
            tokenService.HashToken(refreshToken.Value),
            refreshToken.ExpiresAtUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthenticationResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles.Select(role => role.Name).ToArray(),
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshToken.Value,
            refreshToken.ExpiresAtUtc);
    }

    public async Task<Result<AuthenticationResponse>> RotateSessionAsync(
        string refreshTokenValue,
        CancellationToken cancellationToken)
    {
        RefreshToken? refreshToken = await refreshTokenRepository.GetAsync(
            tokenService.HashToken(refreshTokenValue),
            cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            return Result.Failure<AuthenticationResponse>(UserErrors.InvalidRefreshToken());
        }

        User? user = await userRepository.GetAsync(refreshToken.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthenticationResponse>(UserErrors.InvalidRefreshToken());
        }

        refreshToken.Revoke(DateTime.UtcNow);

        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<Result> RevokeSessionAsync(
        string refreshTokenValue,
        CancellationToken cancellationToken)
    {
        RefreshToken? refreshToken = await refreshTokenRepository.GetAsync(
            tokenService.HashToken(refreshTokenValue),
            cancellationToken);

        if (refreshToken is not null && refreshToken.IsActive)
        {
            refreshToken.Revoke(DateTime.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
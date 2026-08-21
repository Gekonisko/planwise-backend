using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Services;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Application.Tests;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task CreateSessionAsync_persists_hashed_refresh_token()
    {
        var user = User.Create("user@example.com", "Ada", "Lovelace", "password-hash");
        FakeRefreshTokenRepository refreshTokens = new();
        FakeUnitOfWork unitOfWork = new();
        AuthenticationService service = CreateService(new FakeUserRepository(), refreshTokens, unitOfWork);

        Result<AuthenticationResponse> result = await service.CreateSessionAsync(user, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(refreshTokens.Tokens);
        Assert.Equal("hash:refresh-1", refreshTokens.Tokens[0].TokenHash);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal("refresh-1", result.Value.RefreshToken);
    }

    [Fact]
    public async Task RotateSessionAsync_revokes_old_token_and_creates_new_session()
    {
        var user = User.Create("user@example.com", "Ada", "Lovelace", "password-hash");
        FakeUserRepository users = new();
        users.Add(user);
        FakeRefreshTokenRepository refreshTokens = new();
        refreshTokens.Add(RefreshToken.Create(user.Id, "hash:old-token", DateTime.UtcNow.AddMinutes(5)));
        AuthenticationService service = CreateService(users, refreshTokens, new FakeUnitOfWork());

        Result<AuthenticationResponse> result = await service.RotateSessionAsync("old-token", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("refresh-1", result.Value.RefreshToken);
        Assert.Equal(2, refreshTokens.Tokens.Count);
        Assert.False(refreshTokens.Tokens[0].IsActive);
        Assert.True(refreshTokens.Tokens[1].IsActive);
    }

    [Fact]
    public async Task RotateSessionAsync_rejects_expired_token()
    {
        FakeRefreshTokenRepository refreshTokens = new();
        var user = User.Create("user@example.com", "Ada", "Lovelace", "password-hash");
        refreshTokens.Add(RefreshToken.Create(user.Id, "hash:expired", DateTime.UtcNow.AddMinutes(-1)));
        AuthenticationService service = CreateService(new FakeUserRepository(), refreshTokens, new FakeUnitOfWork());

        Result<AuthenticationResponse> result = await service.RotateSessionAsync("expired", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);
    }

    [Fact]
    public async Task RevokeSessionAsync_revokes_active_token()
    {
        FakeRefreshTokenRepository refreshTokens = new();
        var user = User.Create("user@example.com", "Ada", "Lovelace", "password-hash");
        refreshTokens.Add(RefreshToken.Create(user.Id, "hash:logout", DateTime.UtcNow.AddMinutes(5)));
        AuthenticationService service = CreateService(new FakeUserRepository(), refreshTokens, new FakeUnitOfWork());

        Result result = await service.RevokeSessionAsync("logout", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(refreshTokens.Tokens[0].IsActive);
    }

    private static AuthenticationService CreateService(
        FakeUserRepository users,
        FakeRefreshTokenRepository refreshTokens,
        FakeUnitOfWork unitOfWork) =>
        new(users, refreshTokens, unitOfWork, new FakeTokenService());
}
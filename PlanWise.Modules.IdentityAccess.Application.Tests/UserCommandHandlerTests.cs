using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Users.Login;
using PlanWise.Modules.IdentityAccess.Application.Users.Register;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;
using PlanWise.Modules.IdentityAccess.Application.Services;

namespace PlanWise.Modules.IdentityAccess.Application.Tests;

public sealed class UserCommandHandlerTests
{
    [Fact]
    public async Task RegisterUser_normalizes_email_assigns_user_role_and_creates_session()
    {
        FakeUserRepository users = new();
        FakeRefreshTokenRepository refreshTokens = new();
        FakeUnitOfWork unitOfWork = new();
        RegisterUserCommandHandler handler = new(
            users,
            new FakePasswordHasher(),
            new AuthenticationService(users, refreshTokens, unitOfWork, new FakeTokenService()));

        Result<AuthenticationResponse> result = await handler.Handle(
            new RegisterUserCommand(" ADA@EXAMPLE.COM ", " Ada ", " Lovelace ", "correct horse battery staple"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(users.Users);
        Assert.Equal("ada@example.com", users.Users[0].Email);
        Assert.Equal("Ada", users.Users[0].FirstName);
        Assert.Contains(users.Users[0].Roles, role => role.Name == "User");
        Assert.Equal("hashed:correct horse battery staple", users.Users[0].PasswordHash);
        Assert.Single(refreshTokens.Tokens);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task RegisterUser_rejects_duplicate_email_without_saving()
    {
        FakeUserRepository users = new();
        users.Add(PlanWise.Modules.IdentityAccess.Domain.Users.User.Create(
            "ada@example.com", "Ada", "Lovelace", "hash"));
        FakeUnitOfWork unitOfWork = new();
        RegisterUserCommandHandler handler = new(
            users,
            new FakePasswordHasher(),
            new AuthenticationService(users, new FakeRefreshTokenRepository(), unitOfWork, new FakeTokenService()));

        Result<AuthenticationResponse> result = await handler.Handle(
            new RegisterUserCommand("ada@example.com", "Another", "User", "password"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("User.Conflict", result.Error.Code);
        Assert.Single(users.Users);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Login_rejects_invalid_password()
    {
        FakeUserRepository users = new();
        users.Add(PlanWise.Modules.IdentityAccess.Domain.Users.User.Create(
            "ada@example.com", "Ada", "Lovelace", "hashed:correct"));
        LoginCommandHandler handler = new(
            users,
            new FakePasswordHasher(),
            new AuthenticationService(users, new FakeRefreshTokenRepository(), new FakeUnitOfWork(), new FakeTokenService()));

        Result<AuthenticationResponse> result = await handler.Handle(
            new LoginCommand("ada@example.com", "wrong"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
    }
}
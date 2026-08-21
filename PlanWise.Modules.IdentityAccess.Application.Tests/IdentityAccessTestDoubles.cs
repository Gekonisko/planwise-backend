using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Data;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Application.Tests;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> users = [];

    public IReadOnlyList<User> Users => users;

    public Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.SingleOrDefault(user => user.Id == userId));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.SingleOrDefault(user => user.Email == email));

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.Any(user => user.Email == email));

    public void Create(User user) => users.Add(user);

    public void Add(User user) => users.Add(user);
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public Task<RefreshToken?> GetAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens.SingleOrDefault(token => token.TokenHash == tokenHash));

    public Task RevokeAllAsync(Guid userId, DateTime revokedOnUtc, CancellationToken cancellationToken = default)
    {
        foreach (RefreshToken token in Tokens.Where(token => token.UserId == userId && token.IsActive))
        {
            token.Revoke(revokedOnUtc);
        }

        return Task.CompletedTask;
    }

    public void Add(RefreshToken refreshToken) => Tokens.Add(refreshToken);
}

internal sealed class FakePasswordResetTokenRepository : IPasswordResetTokenRepository
{
    public List<PasswordResetToken> Tokens { get; } = [];

    public Task<PasswordResetToken?> GetAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tokens.SingleOrDefault(token => token.TokenHash == tokenHash));

    public void Add(PasswordResetToken passwordResetToken) => Tokens.Add(passwordResetToken);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
}

internal sealed class FakeTokenService : ITokenService
{
    private int tokenNumber;

    public AccessToken CreateAccessToken(User user) =>
        new($"access-{user.Id}", DateTime.UtcNow.AddMinutes(15));

    public RefreshTokenData CreateRefreshToken() =>
        new($"refresh-{++tokenNumber}", DateTime.UtcNow.AddDays(30));

    public RefreshTokenData CreatePasswordResetToken() =>
        new($"reset-{++tokenNumber}", DateTime.UtcNow.AddMinutes(30));

    public string HashToken(string token) => $"hash:{token}";
}

internal sealed class FakeUserContext(Guid? userId) : IUserContext
{
    public Guid? UserId { get; } = userId;
}
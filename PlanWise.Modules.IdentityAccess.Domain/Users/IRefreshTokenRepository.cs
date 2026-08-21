namespace PlanWise.Modules.IdentityAccess.Domain.Users;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task RevokeAllAsync(Guid userId, DateTime revokedOnUtc, CancellationToken cancellationToken = default);

    void Add(RefreshToken refreshToken);
}
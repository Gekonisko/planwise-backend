using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.IdentityAccess.Domain.Users;
using PlanWise.Modules.IdentityAccess.Infrastructure.Database;

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Users;

internal sealed class RefreshTokenRepository(IdentityAccessDbContext dbContext) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeAllAsync(Guid userId, DateTime revokedOnUtc, CancellationToken cancellationToken = default)
    {
        List<RefreshToken> tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedOnUtc == null)
            .ToListAsync(cancellationToken);

        foreach (RefreshToken token in tokens)
        {
            token.Revoke(revokedOnUtc);
        }
    }

    public void Add(RefreshToken refreshToken) => dbContext.RefreshTokens.Add(refreshToken);
}
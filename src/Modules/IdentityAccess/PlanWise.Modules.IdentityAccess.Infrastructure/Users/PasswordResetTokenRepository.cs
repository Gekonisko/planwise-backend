using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.IdentityAccess.Domain.Users;
using PlanWise.Modules.IdentityAccess.Infrastructure.Database;

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Users;

internal sealed class PasswordResetTokenRepository(IdentityAccessDbContext dbContext) : IPasswordResetTokenRepository
{
    public async Task<PasswordResetToken?> GetAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await dbContext.PasswordResetTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public void Add(PasswordResetToken passwordResetToken) => dbContext.PasswordResetTokens.Add(passwordResetToken);
}
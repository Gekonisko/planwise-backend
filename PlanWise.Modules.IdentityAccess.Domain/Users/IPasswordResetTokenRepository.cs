namespace PlanWise.Modules.IdentityAccess.Domain.Users;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetAsync(string tokenHash, CancellationToken cancellationToken = default);

    void Add(PasswordResetToken passwordResetToken);
}
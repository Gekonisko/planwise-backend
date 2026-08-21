namespace PlanWise.Modules.IdentityAccess.Domain.Users;

public sealed class PasswordResetToken
{
    private PasswordResetToken()
    {
    }

    private PasswordResetToken(Guid userId, string tokenHash, DateTime expiresOnUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresOnUtc = expiresOnUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime? UsedOnUtc { get; private set; }

    public bool IsActive => UsedOnUtc is null && ExpiresOnUtc > DateTime.UtcNow;

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTime expiresOnUtc) =>
        new(userId, tokenHash, expiresOnUtc);

    public void MarkUsed(DateTime usedOnUtc) => UsedOnUtc = usedOnUtc;
}
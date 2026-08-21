namespace PlanWise.Modules.IdentityAccess.Domain.Users;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    private RefreshToken(Guid userId, string tokenHash, DateTime expiresOnUtc)
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
    public DateTime? RevokedOnUtc { get; private set; }

    public bool IsActive => RevokedOnUtc is null && ExpiresOnUtc > DateTime.UtcNow;

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresOnUtc) =>
        new(userId, tokenHash, expiresOnUtc);

    public void Revoke(DateTime revokedOnUtc) => RevokedOnUtc = revokedOnUtc;
}
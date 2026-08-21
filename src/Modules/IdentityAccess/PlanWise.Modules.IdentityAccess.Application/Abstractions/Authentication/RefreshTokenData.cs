namespace PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;

public sealed record RefreshTokenData(string Value, DateTime ExpiresAtUtc);
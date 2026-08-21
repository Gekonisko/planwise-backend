namespace PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
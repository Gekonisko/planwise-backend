namespace PlanWise.Modules.IdentityAccess.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PlanWise";
    public string Audience { get; set; } = "PlanWise.Web";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
    public int PasswordResetMinutes { get; set; } = 30;
}
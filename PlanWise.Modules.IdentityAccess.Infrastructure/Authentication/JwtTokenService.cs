using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Domain.Users;

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Authentication;

internal sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions options = options.Value;

    public AccessToken CreateAccessToken(User user)
    {
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(options.AccessTokenMinutes);
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            .. user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Name))
        ];

        SigningCredentials credentials = new(
            new SymmetricSecurityKey(GetSecretKey()),
            SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public RefreshTokenData CreateRefreshToken() => CreateOpaqueToken(options.RefreshTokenDays);

    public RefreshTokenData CreatePasswordResetToken() => CreateOpaqueToken(0, options.PasswordResetMinutes);

    public string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private RefreshTokenData CreateOpaqueToken(int days, int minutes = 0)
    {
        string value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        DateTime expiresAtUtc = DateTime.UtcNow.AddDays(days).AddMinutes(minutes);

        return new RefreshTokenData(value, expiresAtUtc);
    }

    private byte[] GetSecretKey()
    {
        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new InvalidOperationException("Authentication:Jwt:SecretKey is not configured");
        }

        return Encoding.UTF8.GetBytes(options.SecretKey);
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Authentication;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid? UserId
    {
        get
        {
            string? value = httpContextAccessor.HttpContext?.User.FindFirstValue(
                JwtRegisteredClaimNames.Sub) ??
                httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out Guid userId) ? userId : null;
        }
    }
}
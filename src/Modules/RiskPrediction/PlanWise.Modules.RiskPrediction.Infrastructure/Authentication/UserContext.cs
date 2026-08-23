using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Authentication;

namespace PlanWise.Modules.RiskPrediction.Infrastructure.Authentication;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid? UserId
    {
        get
        {
            string? value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out Guid userId) ? userId : null;
        }
    }

    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);
}

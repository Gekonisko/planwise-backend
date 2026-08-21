namespace PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;

public interface IPasswordResetSender
{
    Task SendAsync(string email, string token, CancellationToken cancellationToken = default);
}
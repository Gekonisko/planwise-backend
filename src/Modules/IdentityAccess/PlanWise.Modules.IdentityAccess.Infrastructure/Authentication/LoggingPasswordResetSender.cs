using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Authentication;

internal sealed class LoggingPasswordResetSender(
    ILogger<LoggingPasswordResetSender> logger,
    IOptions<PasswordResetOptions> options) : IPasswordResetSender
{
    public Task SendAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        string link = $"{options.Value.FrontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
        logger.LogInformation("Password reset link generated for {Email}: {Link}", email, link);
        return Task.CompletedTask;
    }
}

public sealed class PasswordResetOptions
{
    public const string SectionName = "Authentication:PasswordReset";

    public string FrontendUrl { get; set; } = "http://localhost:4200";
}
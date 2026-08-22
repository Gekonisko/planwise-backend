using PlanWise.Common.Domain;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.IdentityAccess.Application.Services;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Logout;

internal sealed class LogoutCommandHandler(AuthenticationService authenticationService)
    : ICommandHandler<LogoutCommand>
{
    public Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        authenticationService.RevokeSessionAsync(request.Token, cancellationToken);
}
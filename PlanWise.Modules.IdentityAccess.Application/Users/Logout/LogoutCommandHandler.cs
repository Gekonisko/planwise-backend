using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;
using PlanWise.Modules.IdentityAccess.Application.Services;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Logout;

internal sealed class LogoutCommandHandler(AuthenticationService authenticationService)
    : ICommandHandler<LogoutCommand>
{
    public Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        authenticationService.RevokeSessionAsync(request.Token, cancellationToken);
}
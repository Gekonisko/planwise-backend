using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;
using PlanWise.Modules.IdentityAccess.Application.Services;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Refresh;

internal sealed class RefreshTokenCommandHandler(AuthenticationService authenticationService)
    : ICommandHandler<RefreshTokenCommand, AuthenticationResponse>
{
    public Task<Result<AuthenticationResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken) =>
        authenticationService.RotateSessionAsync(request.Token, cancellationToken);
}
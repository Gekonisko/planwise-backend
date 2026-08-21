using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Refresh;

public sealed record RefreshTokenCommand(string Token) : ICommand<AuthenticationResponse>;
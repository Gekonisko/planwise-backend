using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Refresh;

public sealed record RefreshTokenCommand(string Token) : ICommand<AuthenticationResponse>;
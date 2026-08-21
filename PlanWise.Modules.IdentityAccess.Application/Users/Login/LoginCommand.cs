using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthenticationResponse>;
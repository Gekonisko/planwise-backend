using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Login;

public sealed record LoginCommand(string Email, string Password, bool RememberMe) : ICommand<AuthenticationResponse>;
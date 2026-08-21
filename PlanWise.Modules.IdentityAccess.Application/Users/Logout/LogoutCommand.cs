using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Logout;

public sealed record LogoutCommand(string Token) : ICommand;
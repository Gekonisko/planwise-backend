using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Logout;

public sealed record LogoutCommand(string Token) : ICommand;
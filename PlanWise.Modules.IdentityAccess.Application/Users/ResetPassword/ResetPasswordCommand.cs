using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string Password) : ICommand;
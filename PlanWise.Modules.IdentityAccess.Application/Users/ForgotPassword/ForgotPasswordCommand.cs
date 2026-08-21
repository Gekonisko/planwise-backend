using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
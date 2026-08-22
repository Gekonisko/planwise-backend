using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
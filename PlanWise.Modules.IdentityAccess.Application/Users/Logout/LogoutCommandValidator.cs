using FluentValidation;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Logout;

internal sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator() => RuleFor(command => command.Token).NotEmpty();
}
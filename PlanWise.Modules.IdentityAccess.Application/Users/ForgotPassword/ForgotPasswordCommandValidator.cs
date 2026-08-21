using FluentValidation;

namespace PlanWise.Modules.IdentityAccess.Application.Users.ForgotPassword;

internal sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator() => RuleFor(command => command.Email).NotEmpty().EmailAddress();
}
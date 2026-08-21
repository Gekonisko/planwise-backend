using FluentValidation;

namespace PlanWise.Modules.IdentityAccess.Application.Users.ResetPassword;

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}
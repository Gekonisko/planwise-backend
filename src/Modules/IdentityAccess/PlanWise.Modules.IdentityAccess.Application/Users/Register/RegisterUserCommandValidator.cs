using FluentValidation;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Register;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(300);
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}
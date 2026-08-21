using FluentValidation;

namespace PlanWise.Modules.IdentityAccess.Application.Users.CreateUser;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(u => u.Email).EmailAddress().NotEmpty().NotNull();
        RuleFor(u => u.Password).NotEmpty().NotNull();
    }
}

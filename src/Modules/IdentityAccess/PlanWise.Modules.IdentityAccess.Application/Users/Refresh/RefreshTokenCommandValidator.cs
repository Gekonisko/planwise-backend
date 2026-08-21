using FluentValidation;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Refresh;

internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(command => command.Token).NotEmpty();
}
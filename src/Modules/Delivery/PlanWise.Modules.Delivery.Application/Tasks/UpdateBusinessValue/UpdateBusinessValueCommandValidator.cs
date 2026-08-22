using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Tasks.UpdateBusinessValue;

internal sealed class UpdateBusinessValueCommandValidator : AbstractValidator<UpdateBusinessValueCommand>
{
    public UpdateBusinessValueCommandValidator()
    {
        RuleFor(command => command.BusinessValue).InclusiveBetween(0, 100);
    }
}

using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Sprints.UpdateSprint;

internal sealed class UpdateSprintCommandValidator : AbstractValidator<UpdateSprintCommand>
{
    public UpdateSprintCommandValidator()
    {
        RuleFor(command => command.Name).MaximumLength(200).When(command => command.Name is not null);
        RuleFor(command => command.Goal).MaximumLength(1000).When(command => command.Goal is not null);
        RuleFor(command => command.EndDate)
            .GreaterThanOrEqualTo(command => command.StartDate!.Value)
            .When(command => command.StartDate is not null && command.EndDate is not null);
    }
}

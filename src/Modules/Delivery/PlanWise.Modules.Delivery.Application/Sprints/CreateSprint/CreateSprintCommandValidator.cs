using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Sprints.CreateSprint;

internal sealed class CreateSprintCommandValidator : AbstractValidator<CreateSprintCommand>
{
    public CreateSprintCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Goal).MaximumLength(1000);
        RuleFor(command => command.EndDate).GreaterThanOrEqualTo(command => command.StartDate);
    }
}

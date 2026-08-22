using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Tasks.Subtasks;

internal sealed class AddSubtaskCommandValidator : AbstractValidator<AddSubtaskCommand>
{
    public AddSubtaskCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(300);
    }
}

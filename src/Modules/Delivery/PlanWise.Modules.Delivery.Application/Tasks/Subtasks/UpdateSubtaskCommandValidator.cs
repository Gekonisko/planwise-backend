using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Tasks.Subtasks;

internal sealed class UpdateSubtaskCommandValidator : AbstractValidator<UpdateSubtaskCommand>
{
    public UpdateSubtaskCommandValidator()
    {
        RuleFor(command => command.Title).MaximumLength(300).When(command => command.Title is not null);
    }
}

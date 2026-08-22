using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Tasks.Links;

internal sealed class AddTaskLinkCommandValidator : AbstractValidator<AddTaskLinkCommand>
{
    public AddTaskLinkCommandValidator()
    {
        RuleFor(command => command.Type).Must(type => Enum.TryParse<Domain.Tasks.TaskLinkType>(type, ignoreCase: true, out _));
        RuleFor(command => command).Must(command => command.LinkedTaskId != command.TaskId)
            .WithMessage("A task cannot link to itself");
    }
}

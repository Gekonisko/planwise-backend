using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Tasks.UpdateTask;

internal sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(command => command.Title).MaximumLength(300).When(command => command.Title is not null);
        RuleFor(command => command.Description).MaximumLength(5000).When(command => command.Description is not null);
        RuleFor(command => command.Priority)
            .Must(priority => Enum.TryParse<Domain.Tasks.TaskPriority>(priority, ignoreCase: true, out _))
            .When(command => command.Priority is not null);
        RuleFor(command => command.Points).GreaterThanOrEqualTo(0).When(command => command.Points is not null);
    }
}

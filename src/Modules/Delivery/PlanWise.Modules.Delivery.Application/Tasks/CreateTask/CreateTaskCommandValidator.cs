using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Tasks.CreateTask;

internal sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(300);
        RuleFor(command => command.Description).MaximumLength(5000);
        RuleFor(command => command.Priority).Must(priority => Enum.TryParse<Domain.Tasks.TaskPriority>(priority, ignoreCase: true, out _));
        RuleFor(command => command.Points).GreaterThanOrEqualTo(0).When(command => command.Points is not null);
        RuleFor(command => command.BusinessValue).InclusiveBetween(0, 100).When(command => command.BusinessValue is not null);
    }
}

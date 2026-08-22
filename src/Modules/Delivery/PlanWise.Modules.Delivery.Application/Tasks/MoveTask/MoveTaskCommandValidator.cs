using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Tasks.MoveTask;

internal sealed class MoveTaskCommandValidator : AbstractValidator<MoveTaskCommand>
{
    public MoveTaskCommandValidator()
    {
        RuleFor(command => command.Status).Must(status => Enum.TryParse<Domain.Tasks.ProjectTaskStatus>(status, ignoreCase: true, out _));
        RuleFor(command => command.Index).GreaterThanOrEqualTo(0);
    }
}

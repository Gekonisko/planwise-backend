using FluentValidation;

namespace PlanWise.Modules.Delivery.Application.Tasks.ReorderTasks;

internal sealed class ReorderTasksCommandValidator : AbstractValidator<ReorderTasksCommand>
{
    public ReorderTasksCommandValidator()
    {
        RuleFor(command => command.TaskIds).NotEmpty();
    }
}

using FluentValidation;

namespace PlanWise.Modules.Scheduling.Application.Milestones.CreateMilestone;

internal sealed class CreateMilestoneCommandValidator : AbstractValidator<CreateMilestoneCommand>
{
    public CreateMilestoneCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
    }
}

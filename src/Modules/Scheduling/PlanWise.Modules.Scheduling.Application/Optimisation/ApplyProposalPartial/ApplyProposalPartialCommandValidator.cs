using FluentValidation;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.ApplyProposalPartial;

internal sealed class ApplyProposalPartialCommandValidator : AbstractValidator<ApplyProposalPartialCommand>
{
    public ApplyProposalPartialCommandValidator()
    {
        RuleFor(command => command.AssignmentIds).NotEmpty();
    }
}

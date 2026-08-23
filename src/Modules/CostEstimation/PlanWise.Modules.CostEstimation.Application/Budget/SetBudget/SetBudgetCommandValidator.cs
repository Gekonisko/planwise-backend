using FluentValidation;

namespace PlanWise.Modules.CostEstimation.Application.Budget.SetBudget;

internal sealed class SetBudgetCommandValidator : AbstractValidator<SetBudgetCommand>
{
    public SetBudgetCommandValidator()
    {
        RuleFor(command => command.Amount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
    }
}

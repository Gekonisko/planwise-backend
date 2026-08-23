using FluentValidation;

namespace PlanWise.Modules.Scheduling.Application.Schedule.ValidateSchedule;

internal sealed class ValidateScheduleCommandValidator : AbstractValidator<ValidateScheduleCommand>
{
    public ValidateScheduleCommandValidator()
    {
        RuleFor(command => command.Moves).NotEmpty();
        RuleForEach(command => command.Moves).ChildRules(move =>
            move.RuleFor(m => m.EndDate).GreaterThanOrEqualTo(m => m.StartDate));
    }
}

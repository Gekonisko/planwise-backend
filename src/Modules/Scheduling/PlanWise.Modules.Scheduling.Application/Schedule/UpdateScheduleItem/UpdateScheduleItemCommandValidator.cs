using FluentValidation;

namespace PlanWise.Modules.Scheduling.Application.Schedule.UpdateScheduleItem;

internal sealed class UpdateScheduleItemCommandValidator : AbstractValidator<UpdateScheduleItemCommand>
{
    public UpdateScheduleItemCommandValidator()
    {
        RuleFor(command => command.EndDate).GreaterThanOrEqualTo(command => command.StartDate);
    }
}

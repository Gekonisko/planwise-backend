using FluentValidation;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

internal sealed class AddProjectMemberCommandValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(300);
        RuleFor(command => command.Role).NotEmpty().MaximumLength(50);
        RuleFor(command => command.Capacity).InclusiveBetween(0, 1);
        RuleFor(command => command.HourlyRate).GreaterThanOrEqualTo(0);
    }
}
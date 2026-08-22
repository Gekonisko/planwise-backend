using FluentValidation;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

internal sealed class UpdateProjectMemberCommandValidator : AbstractValidator<UpdateProjectMemberCommand>
{
    public UpdateProjectMemberCommandValidator()
    {
        RuleFor(command => command.Role).NotEmpty().MaximumLength(50);
        RuleFor(command => command.Capacity).InclusiveBetween(0, 1);
        RuleFor(command => command.HourlyRate).GreaterThanOrEqualTo(0);
    }
}

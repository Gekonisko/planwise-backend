using FluentValidation;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.UpdateProject;

internal sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200).When(command => command.Name is not null);
        RuleFor(command => command.Process)
            .Must(process => process is "scrum" or "kanban")
            .When(command => command.Process is not null);
        RuleFor(command => command.ClientName).MaximumLength(200);
        RuleFor(command => command.Status)
            .Must(status => Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out _))
            .When(command => command.Status is not null)
            .WithMessage("Status must be one of: Active, OnHold, Completed, Archived.");
    }
}

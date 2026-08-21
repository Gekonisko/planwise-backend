using FluentValidation;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.UpdateProject;

internal sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Process).Must(process => process is "scrum" or "kanban");
    }
}
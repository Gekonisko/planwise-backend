using FluentValidation;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.CreateProject;

internal sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.KeyPrefix).NotEmpty().Matches("^[A-Z][A-Z0-9]{1,9}$");
        RuleFor(command => command.Process).Must(process => process is "scrum" or "kanban");
        RuleFor(command => command.ClientName).MaximumLength(200);
    }
}
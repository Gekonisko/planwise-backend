using FluentValidation;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members.Skills;

internal sealed class SetMemberSkillsCommandValidator : AbstractValidator<SetMemberSkillsCommand>
{
    public SetMemberSkillsCommandValidator()
    {
        RuleFor(command => command.Skills).NotNull().Must(skills => skills.Count <= 20)
            .WithMessage("A member can have at most 20 skill tags");

        RuleForEach(command => command.Skills).NotEmpty().MaximumLength(50);
    }
}

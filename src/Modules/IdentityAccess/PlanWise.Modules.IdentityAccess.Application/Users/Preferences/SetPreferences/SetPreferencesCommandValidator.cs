using FluentValidation;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Preferences.SetPreferences;

internal sealed class SetPreferencesCommandValidator : AbstractValidator<SetPreferencesCommand>
{
    private static readonly string[] AllowedGroupings = ["status", "assignee", "priority"];

    public SetPreferencesCommandValidator()
    {
        RuleFor(command => command.BoardGrouping).Must(grouping => AllowedGroupings.Contains(grouping))
            .WithMessage($"Board grouping must be one of: {string.Join(", ", AllowedGroupings)}");
    }
}

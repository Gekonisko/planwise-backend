using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.IdentityAccess.Application.Users.Preferences;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Preferences.SetPreferences;

public sealed record SetPreferencesCommand(string BoardGrouping, bool WipDisplay, Guid? DefaultProjectId)
    : ICommand<PreferencesResponse>;

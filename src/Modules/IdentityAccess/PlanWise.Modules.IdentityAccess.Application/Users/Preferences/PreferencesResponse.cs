namespace PlanWise.Modules.IdentityAccess.Application.Users.Preferences;

public sealed record PreferencesResponse(string BoardGrouping, bool WipDisplay, Guid? DefaultProjectId);

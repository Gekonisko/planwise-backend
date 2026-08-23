using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.IdentityAccess.Application.Users.Preferences;

namespace PlanWise.Modules.IdentityAccess.Application.Users.Preferences.GetPreferences;

public sealed record GetPreferencesQuery : IQuery<PreferencesResponse>;

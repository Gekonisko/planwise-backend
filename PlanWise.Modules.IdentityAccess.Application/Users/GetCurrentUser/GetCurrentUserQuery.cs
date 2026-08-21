using PlanWise.Modules.IdentityAccess.Application.Abstractions.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserResponse>;
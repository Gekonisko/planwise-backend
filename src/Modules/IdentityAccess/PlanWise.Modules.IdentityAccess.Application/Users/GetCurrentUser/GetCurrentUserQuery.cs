using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.IdentityAccess.Application.Users.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserResponse>;
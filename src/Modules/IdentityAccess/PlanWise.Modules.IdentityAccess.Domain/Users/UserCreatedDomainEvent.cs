using PlanWise.Common.Domain;

namespace PlanWise.Modules.IdentityAccess.Domain.Users;

public class UserCreatedDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
}

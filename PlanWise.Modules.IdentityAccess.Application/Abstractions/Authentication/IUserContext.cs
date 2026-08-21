namespace PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid? UserId { get; }
}
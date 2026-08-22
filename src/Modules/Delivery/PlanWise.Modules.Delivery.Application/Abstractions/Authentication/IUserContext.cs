namespace PlanWise.Modules.Delivery.Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid? UserId { get; }

    string? Email { get; }
}

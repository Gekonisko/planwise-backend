namespace PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid? UserId { get; }

    string? Email { get; }
}
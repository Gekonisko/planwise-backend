using PlanWise.Common.Domain;
using PlanWise.Modules.IdentityAccess.Domain.Roles;

namespace PlanWise.Modules.IdentityAccess.Domain.Users;

public sealed class User : Entity
{
    private readonly List<Role> _roles = [];

    public User()
    {
    }

    public string Email { get; private set; }
    public string FirstName { get; private set; }

    public string LastName { get; private set; }
    public string PasswordHash { get; private set; }
    public IReadOnlyCollection<Role> Roles => _roles.ToList();
    public string? BoardGrouping { get; private set; }
    public bool? WipDisplay { get; private set; }
    public Guid? DefaultProjectId { get; private set; }

    public static User Create(string Email, string FirstName, string LastName, string PasswordHash)
    {
        var newUser = new User()
        {
            Id = Guid.NewGuid(),
            Email = Email,
            FirstName = FirstName,
            LastName = LastName,
            PasswordHash = PasswordHash
        };

        newUser.AddRole(Role.User);

        newUser.Raise(new UserCreatedDomainEvent(newUser.Id));

        return newUser;
    }

    public void ChangePassword(string passwordHash) => PasswordHash = passwordHash;

    public void SetPreferences(string boardGrouping, bool wipDisplay, Guid? defaultProjectId)
    {
        BoardGrouping = boardGrouping;
        WipDisplay = wipDisplay;
        DefaultProjectId = defaultProjectId;
    }

    private void AddRole(Role role) => _roles.Add(role);
}

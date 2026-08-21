using PlanWise.Modules.IdentityAccess.Domain.Abstractions;
using PlanWise.Modules.IdentityAccess.Domain.Roles;

namespace PlanWise.Modules.IdentityAccess.Domain.Users;

public sealed class User : Entity
{
    private readonly List<Role> _roles = [];

    public User()
    {
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string FirstName { get; private set; }

    public string LastName { get; private set; }
    public string PasswordHash { get; private set; }
    public IReadOnlyCollection<Role> Roles => _roles.ToList();

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

        newUser.Raise(new UserCreatedDomainEvent(newUser.Id));

        return newUser;
    }
}

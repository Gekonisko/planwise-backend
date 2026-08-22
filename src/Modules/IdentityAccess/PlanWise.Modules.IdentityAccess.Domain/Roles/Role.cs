namespace PlanWise.Modules.IdentityAccess.Domain.Roles;

public sealed class Role
{
    public static readonly Role Admin = new Role("Admin");
    public static readonly Role User = new Role("User");

    private Role()
    {
    }

    private Role(string name)
    {
        Name = name;
    }

    public string Name { get; private set; }
}

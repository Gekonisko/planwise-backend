namespace PlanWise.Modules.IdentityAccess.Domain.Roles;

public static class RolePermissions
{
    private static readonly Dictionary<string, string[]> PermissionsByRole = new()
    {
        ["Admin"] = ["workspace:admin", "projects:manage", "members:manage"],
        ["User"] = ["projects:read", "projects:write"]
    };

    public static string[] For(IEnumerable<string> roleNames) =>
        roleNames
            .SelectMany(roleName => PermissionsByRole.TryGetValue(roleName, out string[]? permissions)
                ? permissions
                : [])
            .Distinct()
            .ToArray();
}

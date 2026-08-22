using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.IdentityAccess.Domain.Roles;
using PlanWise.Modules.IdentityAccess.Domain.Users;
using PlanWise.Modules.IdentityAccess.Infrastructure.Database;

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Users;

public class UserRepository(IdentityAccessDbContext usersDb) : IUserRepository
{
    public void Create(User user)
    {
        // Role.User/Role.Admin are shared static instances seeded by migration; attaching them as
        // Unchanged (instead of letting Add() cascade them as new) stops EF from re-inserting a row
        // that already exists in the roles table.
        foreach (Role role in user.Roles)
        {
            usersDb.Attach(role);
        }

        usersDb.Users.Add(user);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await usersDb.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await usersDb.Users
            .Include(user => user.Roles)
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await usersDb.Users
            .Include(user => user.Roles)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }
}

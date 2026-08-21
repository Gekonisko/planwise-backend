using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.IdentityAccess.Domain.Users;
using PlanWise.Modules.IdentityAccess.Infrastructure.Database;

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Users;

public class UserRepository(IdentityAccessDbContext usersDb) : IUserRepository
{
    public void Create(User user)
    {
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

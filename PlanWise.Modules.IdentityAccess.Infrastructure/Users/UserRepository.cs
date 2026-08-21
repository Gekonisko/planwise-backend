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
        return await usersDb.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken) is not null;
    }

    public async Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await usersDb.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }
}

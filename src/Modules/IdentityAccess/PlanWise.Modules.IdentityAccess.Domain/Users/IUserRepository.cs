namespace PlanWise.Modules.IdentityAccess.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    void Create(User user);
}

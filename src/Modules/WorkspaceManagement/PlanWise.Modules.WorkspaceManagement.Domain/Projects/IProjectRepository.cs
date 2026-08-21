namespace PlanWise.Modules.WorkspaceManagement.Domain.Projects;

public interface IProjectRepository
{
    Task<Project?> GetAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Project?> GetForUserAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByKeyPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);
    void Add(Project project);
}
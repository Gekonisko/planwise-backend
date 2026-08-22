namespace PlanWise.Modules.WorkspaceManagement.Domain.Projects;

public interface IProjectRepository
{
    Task<Project?> GetAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Project?> GetForUserAsync(Guid projectId, Guid userId, string? email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetForUserAsync(Guid userId, string? email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByKeyPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);
    void Add(Project project);
    void AddMember(ProjectMember member);
}
using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;
using PlanWise.Modules.WorkspaceManagement.Infrastructure.Database;

namespace PlanWise.Modules.WorkspaceManagement.Infrastructure.Projects;

internal sealed class ProjectRepository(WorkspaceManagementDbContext dbContext) : IProjectRepository
{
    public async Task<Project?> GetAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await dbContext.Projects
            .Include(project => project.Members)
            .Include(project => project.Labels)
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken);

    public async Task<Project?> GetForUserAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Projects
            .Include(project => project.Members)
            .Include(project => project.Labels)
            .SingleOrDefaultAsync(
                project => project.Id == projectId &&
                    (project.OwnerId == userId || project.Members.Any(member => member.UserId == userId)),
                cancellationToken);

    public async Task<IReadOnlyList<Project>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Projects
            .Include(project => project.Members)
            .Include(project => project.Labels)
            .Where(project => project.OwnerId == userId || project.Members.Any(member => member.UserId == userId))
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByKeyPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default) =>
        dbContext.Projects.AnyAsync(project => project.KeyPrefix == keyPrefix, cancellationToken);

    public void Add(Project project) => dbContext.Projects.Add(project);
}
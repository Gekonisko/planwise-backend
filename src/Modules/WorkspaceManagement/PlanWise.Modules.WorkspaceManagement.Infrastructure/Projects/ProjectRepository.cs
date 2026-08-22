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

    public async Task<Project?> GetForUserAsync(Guid projectId, Guid userId, string? email, CancellationToken cancellationToken = default) =>
        await dbContext.Projects
            .Include(project => project.Members)
            .Include(project => project.Labels)
            .SingleOrDefaultAsync(
                project => project.Id == projectId &&
                    (project.OwnerId == userId ||
                     project.Members.Any(member => member.UserId == userId ||
                         member.UserId == null && email != null && member.Email == email)),
                cancellationToken);

    public async Task<IReadOnlyList<Project>> GetForUserAsync(Guid userId, string? email, CancellationToken cancellationToken = default) =>
        await dbContext.Projects
            .Include(project => project.Members)
            .Include(project => project.Labels)
            .Where(project => project.OwnerId == userId ||
                project.Members.Any(member => member.UserId == userId ||
                    member.UserId == null && email != null && member.Email == email))
            .OrderBy(project => project.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByKeyPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default) =>
        dbContext.Projects.AnyAsync(project => project.KeyPrefix == keyPrefix, cancellationToken);

    public void Add(Project project) => dbContext.Projects.Add(project);

    // A member added to an already-tracked (loaded) Project isn't reachable via an explicit Add() cascade,
    // so EF's change detection would otherwise mistake this new row (its id is already client-generated) for
    // a modified existing one and emit an UPDATE instead of an INSERT.
    public void AddMember(ProjectMember member) => dbContext.Add(member);
}
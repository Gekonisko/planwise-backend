using Microsoft.EntityFrameworkCore;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.WorkspaceManagement.Infrastructure.Database;

namespace PlanWise.Modules.WorkspaceManagement.Infrastructure.Projects;

internal sealed class ProjectAccessService(WorkspaceManagementDbContext dbContext) : IProjectAccessService
{
    public Task<bool> HasAccessAsync(Guid projectId, Guid userId, string? email, CancellationToken cancellationToken = default) =>
        dbContext.Projects.AnyAsync(
            project => project.Id == projectId &&
                (project.OwnerId == userId ||
                 project.Members.Any(member => member.UserId == userId ||
                     member.UserId == null && email != null && member.Email == email)),
            cancellationToken);

    public Task<string?> GetKeyPrefixAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        dbContext.Projects
            .Where(project => project.Id == projectId)
            .Select(project => project.KeyPrefix)
            .SingleOrDefaultAsync(cancellationToken);
}

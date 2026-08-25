using Microsoft.EntityFrameworkCore;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.WorkspaceManagement.Infrastructure.Database;

namespace PlanWise.Modules.WorkspaceManagement.Infrastructure.Projects;

internal sealed class ProjectAccessService(WorkspaceManagementDbContext dbContext) : IProjectAccessService, IProjectMembersService
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

    public Task<ProjectInfo?> GetProjectInfoAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        dbContext.Projects
            .Where(project => project.Id == projectId)
            .Select(project => new ProjectInfo(project.Name, project.ClientName))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectMemberSummary>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await dbContext.ProjectMembers
            .Where(member => member.ProjectId == projectId)
            .Select(member => new ProjectMemberSummary(member.UserId, member.Email, member.Capacity, member.Skills))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectSearchSummary>> GetAccessibleProjectsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Projects
            .Where(project => project.OwnerId == userId || project.Members.Any(member => member.UserId == userId))
            .Select(project => new ProjectSearchSummary(project.Id, project.Name, project.KeyPrefix))
            .ToListAsync(cancellationToken);
}

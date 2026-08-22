using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Infrastructure.Database;

namespace PlanWise.Modules.Delivery.Infrastructure.Sprints;

internal sealed class SprintRepository(DeliveryDbContext dbContext) : ISprintRepository
{
    public Task<Sprint?> GetAsync(Guid sprintId, CancellationToken cancellationToken = default) =>
        dbContext.Sprints.SingleOrDefaultAsync(sprint => sprint.Id == sprintId, cancellationToken);

    public Task<Sprint?> GetForProjectAsync(Guid sprintId, Guid projectId, CancellationToken cancellationToken = default) =>
        dbContext.Sprints.SingleOrDefaultAsync(sprint => sprint.Id == sprintId && sprint.ProjectId == projectId, cancellationToken);

    public async Task<IReadOnlyList<Sprint>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        await dbContext.Sprints
            .Where(sprint => sprint.ProjectId == projectId)
            .OrderBy(sprint => sprint.StartDate)
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveSprintAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        dbContext.Sprints.AnyAsync(sprint => sprint.ProjectId == projectId && sprint.State == SprintState.Active, cancellationToken);

    public void Add(Sprint sprint) => dbContext.Sprints.Add(sprint);
}

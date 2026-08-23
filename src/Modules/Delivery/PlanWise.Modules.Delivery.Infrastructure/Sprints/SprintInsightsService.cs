using Microsoft.EntityFrameworkCore;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Infrastructure.Database;

namespace PlanWise.Modules.Delivery.Infrastructure.Sprints;

internal sealed class SprintInsightsService(DeliveryDbContext dbContext) : ISprintInsightsService
{
    public async Task<IReadOnlyList<SprintInsightSummary>> GetSprintsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        List<Sprint> sprints = await dbContext.Sprints
            .Where(sprint => sprint.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        return sprints.Select(ToSummary).ToList();
    }

    public async Task<SprintInsightSummary?> GetSprintAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        Sprint? sprint = await dbContext.Sprints.SingleOrDefaultAsync(s => s.Id == sprintId, cancellationToken);
        return sprint is null ? null : ToSummary(sprint);
    }

    private static SprintInsightSummary ToSummary(Sprint sprint) =>
        new(sprint.Id, sprint.ProjectId, sprint.Name, sprint.StartDate, sprint.EndDate, sprint.State.ToString());
}

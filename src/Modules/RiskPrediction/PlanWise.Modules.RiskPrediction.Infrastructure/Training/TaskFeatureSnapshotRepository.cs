using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.RiskPrediction.Domain.Training;
using PlanWise.Modules.RiskPrediction.Infrastructure.Database;

namespace PlanWise.Modules.RiskPrediction.Infrastructure.Training;

internal sealed class TaskFeatureSnapshotRepository(RiskPredictionDbContext dbContext) : ITaskFeatureSnapshotRepository
{
    public void AddRange(IEnumerable<TaskFeatureSnapshot> snapshots) =>
        dbContext.TaskFeatureSnapshots.AddRange(snapshots);

    // Censored rows are returned alongside Pending ones: a task that was open-and-overdue may have
    // finished since, which turns its lower-bound day count into a real one.
    public async Task<IReadOnlyList<TaskFeatureSnapshot>> GetUnresolvedAsync(
        Guid projectId,
        CancellationToken cancellationToken = default) =>
        await dbContext.TaskFeatureSnapshots
            .Where(snapshot => snapshot.ProjectId == projectId &&
                (snapshot.OutcomeStatus == TaskOutcomeStatus.Pending || snapshot.OutcomeIsCensored))
            .ToListAsync(cancellationToken);
}

namespace PlanWise.Modules.RiskPrediction.Domain.Training;

public interface ITaskFeatureSnapshotRepository
{
    void AddRange(IEnumerable<TaskFeatureSnapshot> snapshots);

    /// <summary>
    /// Rows for this project whose outcome is not yet final — Pending, or Late-but-censored because
    /// the task was still open past its due date last time anyone looked.
    /// </summary>
    Task<IReadOnlyList<TaskFeatureSnapshot>> GetUnresolvedAsync(Guid projectId, CancellationToken cancellationToken = default);
}

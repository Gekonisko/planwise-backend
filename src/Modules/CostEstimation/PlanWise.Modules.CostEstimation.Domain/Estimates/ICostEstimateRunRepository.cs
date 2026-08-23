namespace PlanWise.Modules.CostEstimation.Domain.Estimates;

public interface ICostEstimateRunRepository
{
    Task<CostEstimateRun?> GetAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<CostEstimateRun?> GetLatestForProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CostEstimateRun>> GetHistoryForProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    void Add(CostEstimateRun run);
}

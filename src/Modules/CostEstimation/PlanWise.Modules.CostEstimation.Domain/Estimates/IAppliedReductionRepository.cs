namespace PlanWise.Modules.CostEstimation.Domain.Estimates;

public interface IAppliedReductionRepository
{
    Task<IReadOnlyList<AppliedReduction>> GetForRunAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<AppliedReduction?> GetAsync(Guid runId, Guid reductionId, CancellationToken cancellationToken = default);

    void Add(AppliedReduction appliedReduction);

    void Remove(AppliedReduction appliedReduction);
}

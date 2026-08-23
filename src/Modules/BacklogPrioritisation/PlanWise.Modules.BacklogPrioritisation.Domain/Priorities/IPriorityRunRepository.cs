namespace PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

public interface IPriorityRunRepository
{
    Task<PriorityRun?> GetAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<PriorityRun?> GetLatestForProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    void Add(PriorityRun run);
}

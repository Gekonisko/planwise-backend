namespace PlanWise.Modules.Scheduling.Domain.Milestones;

public interface IMilestoneRepository
{
    Task<IReadOnlyList<Milestone>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    void Add(Milestone milestone);
}

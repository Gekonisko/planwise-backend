namespace PlanWise.Modules.Scheduling.Domain.Optimisation;

public interface IScheduleProposalRepository
{
    Task<ScheduleProposal?> GetAsync(Guid proposalId, CancellationToken cancellationToken = default);

    Task<ScheduleProposal?> GetLatestForProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    void Add(ScheduleProposal proposal);
}

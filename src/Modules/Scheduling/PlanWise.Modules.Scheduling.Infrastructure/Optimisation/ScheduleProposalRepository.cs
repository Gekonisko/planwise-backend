using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.Scheduling.Domain.Optimisation;
using PlanWise.Modules.Scheduling.Infrastructure.Database;

namespace PlanWise.Modules.Scheduling.Infrastructure.Optimisation;

internal sealed class ScheduleProposalRepository(SchedulingDbContext dbContext) : IScheduleProposalRepository
{
    public Task<ScheduleProposal?> GetAsync(Guid proposalId, CancellationToken cancellationToken = default) =>
        dbContext.ScheduleProposals
            .Include(proposal => proposal.Assignments)
            .SingleOrDefaultAsync(proposal => proposal.Id == proposalId, cancellationToken);

    public Task<ScheduleProposal?> GetLatestForProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        dbContext.ScheduleProposals
            .Include(proposal => proposal.Assignments)
            .Where(proposal => proposal.ProjectId == projectId)
            .OrderByDescending(proposal => proposal.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(ScheduleProposal proposal) => dbContext.ScheduleProposals.Add(proposal);
}

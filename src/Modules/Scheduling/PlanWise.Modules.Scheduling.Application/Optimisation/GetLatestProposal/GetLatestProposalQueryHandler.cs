using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Domain.Optimisation;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.GetLatestProposal;

internal sealed class GetLatestProposalQueryHandler(
    IScheduleProposalRepository proposalRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetLatestProposalQuery, ProposalResponse>
{
    public async Task<Result<ProposalResponse>> Handle(GetLatestProposalQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<ProposalResponse>(ScheduleErrors.ProjectNotFound(request.ProjectId));
        }

        ScheduleProposal? proposal = await proposalRepository.GetLatestForProjectAsync(request.ProjectId, cancellationToken);
        if (proposal is null)
        {
            return Result.Failure<ProposalResponse>(ScheduleErrors.NoProposalForProject(request.ProjectId));
        }

        return Result.Success(ProposalMappings.ToResponse(proposal));
    }
}

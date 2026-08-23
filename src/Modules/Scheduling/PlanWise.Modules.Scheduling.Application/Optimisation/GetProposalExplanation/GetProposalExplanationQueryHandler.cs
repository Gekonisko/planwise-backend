using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Domain.Optimisation;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.GetProposalExplanation;

internal sealed class GetProposalExplanationQueryHandler(
    IScheduleProposalRepository proposalRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetProposalExplanationQuery, ProposalExplanationResponse>
{
    public async Task<Result<ProposalExplanationResponse>> Handle(GetProposalExplanationQuery request, CancellationToken cancellationToken)
    {
        ScheduleProposal? proposal = await proposalRepository.GetAsync(request.ProposalId, cancellationToken);
        if (proposal is null)
        {
            return Result.Failure<ProposalExplanationResponse>(ScheduleErrors.ProposalNotFound(request.ProposalId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(proposal.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<ProposalExplanationResponse>(ScheduleErrors.ProposalNotFound(request.ProposalId));
        }

        return Result.Success(ProposalMappings.ToExplanationResponse(proposal));
    }
}

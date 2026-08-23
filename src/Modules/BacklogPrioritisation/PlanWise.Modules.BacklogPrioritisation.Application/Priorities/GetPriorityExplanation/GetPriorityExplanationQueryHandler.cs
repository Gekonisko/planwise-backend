using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Authentication;
using PlanWise.Modules.BacklogPrioritisation.Domain;
using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities.GetPriorityExplanation;

internal sealed class GetPriorityExplanationQueryHandler(
    IPriorityRunRepository runRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetPriorityExplanationQuery, PriorityExplanationResponse>
{
    public async Task<Result<PriorityExplanationResponse>> Handle(GetPriorityExplanationQuery request, CancellationToken cancellationToken)
    {
        PriorityRun? run = await runRepository.GetAsync(request.Id, cancellationToken);
        if (run is null)
        {
            return Result.Failure<PriorityExplanationResponse>(PriorityErrors.RunNotFound(request.Id));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(run.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<PriorityExplanationResponse>(PriorityErrors.RunNotFound(request.Id));
        }

        return Result.Success(PriorityMappings.ToExplanationResponse(run));
    }
}

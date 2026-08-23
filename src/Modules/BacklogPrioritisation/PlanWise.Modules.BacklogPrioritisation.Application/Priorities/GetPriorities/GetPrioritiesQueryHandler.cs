using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Authentication;
using PlanWise.Modules.BacklogPrioritisation.Domain;
using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities.GetPriorities;

internal sealed class GetPrioritiesQueryHandler(
    IPriorityRunRepository runRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetPrioritiesQuery, PrioritiesResponse>
{
    public async Task<Result<PrioritiesResponse>> Handle(GetPrioritiesQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<PrioritiesResponse>(PriorityErrors.ProjectNotFound(request.ProjectId));
        }

        PriorityRun? run = await runRepository.GetLatestForProjectAsync(request.ProjectId, cancellationToken);
        if (run is null)
        {
            return Result.Failure<PrioritiesResponse>(PriorityErrors.NoRunForProject(request.ProjectId));
        }

        return Result.Success(PriorityMappings.ToResponse(run));
    }
}

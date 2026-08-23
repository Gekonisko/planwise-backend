using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Authentication;
using PlanWise.Modules.BacklogPrioritisation.Domain;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities.RunPriorities;

internal sealed class RunPrioritiesCommandHandler(
    IProjectAccessService projectAccessService,
    IAsyncJobService asyncJobService,
    IUserContext userContext)
    : ICommandHandler<RunPrioritiesCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RunPrioritiesCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<Guid>(PriorityErrors.ProjectNotFound(request.ProjectId));
        }

        Guid jobId = await asyncJobService.EnqueueAsync("BacklogPrioritisation", request.ProjectId, cancellationToken);
        return Result.Success(jobId);
    }
}

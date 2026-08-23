using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Authentication;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Data;
using PlanWise.Modules.BacklogPrioritisation.Domain;
using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities.DismissPriorities;

internal sealed class DismissPrioritiesCommandHandler(
    IPriorityRunRepository runRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DismissPrioritiesCommand>
{
    public async Task<Result> Handle(DismissPrioritiesCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure(PriorityErrors.ProjectNotFound(request.ProjectId));
        }

        PriorityRun? run = await runRepository.GetLatestForProjectAsync(request.ProjectId, cancellationToken);
        if (run is null)
        {
            return Result.Failure(PriorityErrors.NoRunForProject(request.ProjectId));
        }

        Result dismissResult = run.Dismiss(request.Reason, dateTimeProvider.UtcNow);
        if (dismissResult.IsFailure)
        {
            return dismissResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

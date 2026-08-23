using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Authentication;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Data;
using PlanWise.Modules.BacklogPrioritisation.Domain;
using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities.ApplyPriorities;

internal sealed class ApplyPrioritiesCommandHandler(
    IPriorityRunRepository runRepository,
    IProjectAccessService projectAccessService,
    IProjectTasksService projectTasksService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ApplyPrioritiesCommand, PrioritiesResponse>
{
    public async Task<Result<PrioritiesResponse>> Handle(ApplyPrioritiesCommand request, CancellationToken cancellationToken)
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

        Result applyResult = run.Apply(dateTimeProvider.UtcNow);
        if (applyResult.IsFailure)
        {
            return Result.Failure<PrioritiesResponse>(applyResult.Error);
        }

        IReadOnlyList<Guid> orderedTaskIds = run.Items.OrderBy(item => item.ProposedPosition).Select(item => item.TaskId).ToList();
        bool reordered = await projectTasksService.ReorderBacklogAsync(request.ProjectId, orderedTaskIds, cancellationToken);
        if (!reordered)
        {
            return Result.Failure<PrioritiesResponse>(PriorityErrors.ReorderFailed(request.ProjectId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(PriorityMappings.ToResponse(run));
    }
}

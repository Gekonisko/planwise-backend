using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.OptimiseSchedule;

internal sealed class OptimiseScheduleCommandHandler(
    IProjectAccessService projectAccessService,
    IAsyncJobService asyncJobService,
    IUserContext userContext)
    : ICommandHandler<OptimiseScheduleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(OptimiseScheduleCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<Guid>(ScheduleErrors.ProjectNotFound(request.ProjectId));
        }

        Guid jobId = await asyncJobService.EnqueueAsync("ScheduleOptimisation", request.ProjectId, cancellationToken);
        return Result.Success(jobId);
    }
}

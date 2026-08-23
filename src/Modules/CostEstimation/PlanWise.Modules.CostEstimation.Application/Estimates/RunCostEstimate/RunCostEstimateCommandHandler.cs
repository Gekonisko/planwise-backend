using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Authentication;
using PlanWise.Modules.CostEstimation.Domain;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.RunCostEstimate;

internal sealed class RunCostEstimateCommandHandler(
    IProjectAccessService projectAccessService,
    IAsyncJobService asyncJobService,
    IUserContext userContext)
    : ICommandHandler<RunCostEstimateCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RunCostEstimateCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<Guid>(CostEstimateErrors.ProjectNotFound(request.ProjectId));
        }

        Guid jobId = await asyncJobService.EnqueueAsync("CostEstimation", request.ProjectId, cancellationToken);
        return Result.Success(jobId);
    }
}

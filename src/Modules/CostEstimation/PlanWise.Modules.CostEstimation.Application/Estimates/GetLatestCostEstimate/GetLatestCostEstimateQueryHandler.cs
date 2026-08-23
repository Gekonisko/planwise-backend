using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Authentication;
using PlanWise.Modules.CostEstimation.Domain;
using PlanWise.Modules.CostEstimation.Domain.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.GetLatestCostEstimate;

internal sealed class GetLatestCostEstimateQueryHandler(
    ICostEstimateRunRepository runRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetLatestCostEstimateQuery, CostEstimateResponse>
{
    public async Task<Result<CostEstimateResponse>> Handle(GetLatestCostEstimateQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<CostEstimateResponse>(CostEstimateErrors.ProjectNotFound(request.ProjectId));
        }

        CostEstimateRun? run = await runRepository.GetLatestForProjectAsync(request.ProjectId, cancellationToken);
        if (run is null)
        {
            return Result.Failure<CostEstimateResponse>(CostEstimateErrors.NoRunForProject(request.ProjectId));
        }

        return Result.Success(CostEstimateMappings.ToResponse(run));
    }
}

using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Authentication;
using PlanWise.Modules.CostEstimation.Domain;
using PlanWise.Modules.CostEstimation.Domain.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.GetCostEstimateExplanation;

internal sealed class GetCostEstimateExplanationQueryHandler(
    ICostEstimateRunRepository runRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetCostEstimateExplanationQuery, CostEstimateExplanationResponse>
{
    public async Task<Result<CostEstimateExplanationResponse>> Handle(GetCostEstimateExplanationQuery request, CancellationToken cancellationToken)
    {
        CostEstimateRun? run = await runRepository.GetAsync(request.RunId, cancellationToken);
        if (run is null)
        {
            return Result.Failure<CostEstimateExplanationResponse>(CostEstimateErrors.RunNotFound(request.RunId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(run.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<CostEstimateExplanationResponse>(CostEstimateErrors.RunNotFound(request.RunId));
        }

        return Result.Success(CostEstimateMappings.ToExplanationResponse(run));
    }
}

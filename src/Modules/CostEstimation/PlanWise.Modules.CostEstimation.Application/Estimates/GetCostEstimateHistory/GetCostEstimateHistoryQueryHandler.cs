using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Authentication;
using PlanWise.Modules.CostEstimation.Domain;
using PlanWise.Modules.CostEstimation.Domain.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.GetCostEstimateHistory;

internal sealed class GetCostEstimateHistoryQueryHandler(
    ICostEstimateRunRepository runRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetCostEstimateHistoryQuery, IReadOnlyList<CostEstimateResponse>>
{
    public async Task<Result<IReadOnlyList<CostEstimateResponse>>> Handle(GetCostEstimateHistoryQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<CostEstimateResponse>>(CostEstimateErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<CostEstimateRun> runs = await runRepository.GetHistoryForProjectAsync(request.ProjectId, cancellationToken);

        IReadOnlyList<CostEstimateResponse> responses = runs.Select(CostEstimateMappings.ToResponse).ToList();
        return Result.Success(responses);
    }
}

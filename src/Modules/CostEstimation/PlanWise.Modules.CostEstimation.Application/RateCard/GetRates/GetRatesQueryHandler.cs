using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Authentication;
using PlanWise.Modules.CostEstimation.Domain;

namespace PlanWise.Modules.CostEstimation.Application.RateCard.GetRates;

internal sealed class GetRatesQueryHandler(
    IRateCardProvider rateCardProvider,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetRatesQuery, ProjectRateCard>
{
    public async Task<Result<ProjectRateCard>> Handle(GetRatesQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<ProjectRateCard>(CostEstimateErrors.ProjectNotFound(request.ProjectId));
        }

        return Result.Success(await rateCardProvider.GetRatesAsync(request.ProjectId, cancellationToken));
    }
}

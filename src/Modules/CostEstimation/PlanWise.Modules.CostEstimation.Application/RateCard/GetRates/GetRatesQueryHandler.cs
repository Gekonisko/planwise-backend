using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.CostEstimation.Application.Abstractions;

namespace PlanWise.Modules.CostEstimation.Application.RateCard.GetRates;

internal sealed class GetRatesQueryHandler(IRateCardProvider rateCardProvider)
    : IQueryHandler<GetRatesQuery, IReadOnlyList<RoleRate>>
{
    public Task<Result<IReadOnlyList<RoleRate>>> Handle(GetRatesQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(rateCardProvider.GetRates()));
}

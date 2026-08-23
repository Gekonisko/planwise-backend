using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.CostEstimation.Application.Abstractions;

namespace PlanWise.Modules.CostEstimation.Application.RateCard.GetRates;

public sealed record GetRatesQuery : IQuery<IReadOnlyList<RoleRate>>;

using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.CostEstimation.Application.RateCard;

namespace PlanWise.Modules.CostEstimation.Application.RateCard.GetRates;

public sealed record GetRatesQuery(Guid ProjectId) : IQuery<ProjectRateCard>;

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.GetLatestForecast;

public sealed record GetLatestForecastQuery(Guid ProjectId) : IQuery<LatestForecastResponse>;

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.GetSprintForecast;

public sealed record GetSprintForecastQuery(Guid SprintId) : IQuery<SprintForecastResponse>;

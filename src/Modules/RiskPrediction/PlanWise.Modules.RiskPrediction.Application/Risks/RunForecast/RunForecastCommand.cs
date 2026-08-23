using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.RunForecast;

public sealed record RunForecastCommand(Guid ProjectId) : ICommand<Guid>;

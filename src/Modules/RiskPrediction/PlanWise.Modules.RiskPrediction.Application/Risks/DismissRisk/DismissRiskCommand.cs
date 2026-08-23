using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.DismissRisk;

public sealed record DismissRiskCommand(Guid Id, string? Reason) : ICommand;

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.GetRiskExplanation;

public sealed record GetRiskExplanationQuery(Guid Id) : IQuery<RiskExplanationResponse>;

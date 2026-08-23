using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.GetRisks;

public sealed record GetRisksQuery(Guid ProjectId) : IQuery<IReadOnlyList<TaskRiskResponse>>;

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.GetTaskRisk;

public sealed record GetTaskRiskQuery(Guid TaskId) : IQuery<TaskRiskResponse>;

using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.CostEstimation.Application.Budget;

namespace PlanWise.Modules.CostEstimation.Application.Budget.GetBudget;

public sealed record GetBudgetQuery(Guid ProjectId) : IQuery<BudgetResponse>;

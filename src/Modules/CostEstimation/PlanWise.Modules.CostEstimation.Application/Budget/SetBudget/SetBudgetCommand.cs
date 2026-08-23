using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.CostEstimation.Application.Budget;

namespace PlanWise.Modules.CostEstimation.Application.Budget.SetBudget;

public sealed record SetBudgetCommand(Guid ProjectId, decimal Amount, string Currency) : ICommand<BudgetResponse>;

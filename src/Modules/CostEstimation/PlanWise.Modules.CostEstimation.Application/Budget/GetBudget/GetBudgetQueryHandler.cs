using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Authentication;
using PlanWise.Modules.CostEstimation.Domain;
using PlanWise.Modules.CostEstimation.Domain.Budget;

namespace PlanWise.Modules.CostEstimation.Application.Budget.GetBudget;

internal sealed class GetBudgetQueryHandler(
    IProjectBudgetRepository budgetRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetBudgetQuery, BudgetResponse>
{
    public async Task<Result<BudgetResponse>> Handle(GetBudgetQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<BudgetResponse>(CostEstimateErrors.ProjectNotFound(request.ProjectId));
        }

        ProjectBudget? budget = await budgetRepository.GetAsync(request.ProjectId, cancellationToken);

        return Result.Success(budget is null
            ? new BudgetResponse(request.ProjectId, 0m, "USD", null)
            : new BudgetResponse(budget.ProjectId, budget.Amount, budget.Currency, budget.UpdatedAtUtc));
    }
}

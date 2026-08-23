using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Authentication;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Data;
using PlanWise.Modules.CostEstimation.Domain;
using PlanWise.Modules.CostEstimation.Domain.Budget;

namespace PlanWise.Modules.CostEstimation.Application.Budget.SetBudget;

internal sealed class SetBudgetCommandHandler(
    IProjectBudgetRepository budgetRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<SetBudgetCommand, BudgetResponse>
{
    public async Task<Result<BudgetResponse>> Handle(SetBudgetCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<BudgetResponse>(CostEstimateErrors.ProjectNotFound(request.ProjectId));
        }

        DateTime now = dateTimeProvider.UtcNow;
        ProjectBudget? budget = await budgetRepository.GetAsync(request.ProjectId, cancellationToken);
        if (budget is null)
        {
            budget = ProjectBudget.Create(request.ProjectId, request.Amount, request.Currency, now);
            budgetRepository.Add(budget);
        }
        else
        {
            budget.Set(request.Amount, request.Currency, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new BudgetResponse(budget.ProjectId, budget.Amount, budget.Currency, budget.UpdatedAtUtc));
    }
}

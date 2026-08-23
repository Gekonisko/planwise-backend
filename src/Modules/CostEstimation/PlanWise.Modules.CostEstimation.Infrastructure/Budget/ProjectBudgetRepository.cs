using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.CostEstimation.Domain.Budget;
using PlanWise.Modules.CostEstimation.Infrastructure.Database;

namespace PlanWise.Modules.CostEstimation.Infrastructure.Budget;

internal sealed class ProjectBudgetRepository(CostEstimationDbContext dbContext) : IProjectBudgetRepository
{
    public Task<ProjectBudget?> GetAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        dbContext.Budgets.SingleOrDefaultAsync(budget => budget.ProjectId == projectId, cancellationToken);

    public void Add(ProjectBudget budget) => dbContext.Budgets.Add(budget);
}

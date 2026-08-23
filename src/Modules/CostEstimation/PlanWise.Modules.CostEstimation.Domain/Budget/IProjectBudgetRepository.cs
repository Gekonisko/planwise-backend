namespace PlanWise.Modules.CostEstimation.Domain.Budget;

public interface IProjectBudgetRepository
{
    Task<ProjectBudget?> GetAsync(Guid projectId, CancellationToken cancellationToken = default);

    void Add(ProjectBudget budget);
}

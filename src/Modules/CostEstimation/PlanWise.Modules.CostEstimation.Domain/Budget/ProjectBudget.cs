using PlanWise.Common.Domain;

namespace PlanWise.Modules.CostEstimation.Domain.Budget;

// Id is deliberately the same as ProjectId — one budget per project, and PUT is an upsert, so there's
// no separate "create a budget" step (mirrors Scheduling's ScheduleItem.Id == TaskId trick).
public sealed class ProjectBudget : Entity
{
    private ProjectBudget()
    {
    }

    private ProjectBudget(Guid projectId, decimal amount, string currency, DateTime updatedAtUtc)
    {
        Id = projectId;
        ProjectId = projectId;
        Amount = amount;
        Currency = currency;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid ProjectId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static ProjectBudget Create(Guid projectId, decimal amount, string currency, DateTime updatedAtUtc) =>
        new(projectId, amount, currency, updatedAtUtc);

    public void Set(decimal amount, string currency, DateTime updatedAtUtc)
    {
        Amount = amount;
        Currency = currency;
        UpdatedAtUtc = updatedAtUtc;
    }
}

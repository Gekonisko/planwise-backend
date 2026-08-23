using PlanWise.Modules.Delivery.Application.Sprints;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Overview;

public sealed record TaskStatusCounts(int Backlog, int Todo, int InProgress, int Done);

public sealed record OverviewResponse(
    TaskStatusCounts TaskCounts,
    int TotalPoints,
    int CompletedPoints,
    IReadOnlyList<TaskResponse> NeedsAttention,
    SprintResponse? ActiveSprint);

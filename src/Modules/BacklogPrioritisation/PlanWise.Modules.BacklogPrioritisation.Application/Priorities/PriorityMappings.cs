using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities;

internal static class PriorityMappings
{
    public static PriorityItemResponse ToResponse(PriorityItem item) =>
        new(
            item.TaskId,
            item.TaskKey,
            item.CurrentPosition,
            item.ProposedPosition,
            item.CurrentPosition - item.ProposedPosition,
            item.ValueScore,
            item.DependencyScore,
            item.ComplexityScore,
            item.RiskScore,
            item.Reason);

    public static PrioritiesResponse ToResponse(PriorityRun run) =>
        new(
            run.Id,
            run.Status.ToString(),
            run.Items.OrderBy(item => item.ProposedPosition).Select(ToResponse).ToList(),
            run.CreatedAtUtc);

    public static PriorityExplanationResponse ToExplanationResponse(PriorityRun run) =>
        new(
            run.Id,
            run.ModelVersion,
            run.Items.OrderBy(item => item.ProposedPosition).Select(ToResponse).ToList(),
            run.CreatedAtUtc);
}

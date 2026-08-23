namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities;

public sealed record PriorityItemResponse(
    Guid TaskId,
    string TaskKey,
    int CurrentPosition,
    int ProposedPosition,
    int DeltaFromCurrent,
    decimal ValueScore,
    decimal DependencyScore,
    decimal ComplexityScore,
    decimal RiskScore,
    string Reason);

public sealed record PrioritiesResponse(
    Guid RunId,
    string Status,
    IReadOnlyList<PriorityItemResponse> Items,
    DateTime CreatedAtUtc);

public sealed record PriorityExplanationResponse(
    Guid RunId,
    string ModelVersion,
    IReadOnlyList<PriorityItemResponse> Items,
    DateTime CreatedAtUtc);

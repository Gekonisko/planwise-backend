namespace PlanWise.Modules.Scheduling.Application.Optimisation;

public sealed record ProposedAssignmentResponse(
    Guid Id,
    Guid TaskId,
    string TaskKey,
    Guid? CurrentAssigneeId,
    Guid ProposedAssigneeId,
    string ProposedAssigneeEmail,
    bool IsApplied);

public sealed record ProposalResponse(
    Guid Id,
    Guid ProjectId,
    Guid JobId,
    string Status,
    IReadOnlyList<ProposedAssignmentResponse> Assignments,
    DateTime CreatedAtUtc);

public sealed record ProposalExplanationResponse(
    Guid Id,
    string ModelName,
    string Objective,
    IReadOnlyList<string> ConstraintsHonoured,
    IReadOnlyList<string> ConstraintsRelaxed,
    string ExpectedGain,
    DateTime GeneratedAtUtc);

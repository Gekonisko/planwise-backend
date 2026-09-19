using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Modules.Scheduling.Application.Abstractions;

// The optimiser boundary, the fourth and last of these seams (after ICostEstimationModel,
// IRiskPredictionModel and IBacklogPrioritisationModel). ScheduleOptimisationJobHandler used to *be*
// the optimiser: it held the greedy loop, the model name and the explanation strings, so replacing
// the algorithm meant rewriting the handler and its persistence alongside it.
//
// Everything upstream of this interface — the job contract, the proposal aggregate, apply and
// apply-partial, the diff the UI renders — is indifferent to how the assignments were chosen.

/// <param name="Today">
/// Passed in rather than read from a clock inside the optimiser, so a run is a pure function of its
/// input and can be replayed. Same rule as IRiskPredictionModel, and for the same reason.
/// </param>
public sealed record ScheduleOptimisationInput(
    Guid ProjectId,
    DateOnly Today,
    IReadOnlyList<ScheduleTaskSummary> Tasks,
    IReadOnlyList<ProjectMemberSummary> Members);

public sealed record ProposedTaskAssignment(
    Guid TaskId,
    string TaskKey,
    Guid? CurrentAssigneeId,
    Guid ProposedAssigneeId,
    string ProposedAssigneeEmail);

/// <param name="ModelName">
/// In the result rather than on the interface, unlike the other three seams. A solver can fail to
/// reach any solution inside its time limit, or its native library can fail to load, and the honest
/// answer in that case is a worse schedule produced by a different method — so which engine actually
/// ran is a per-run fact, and it is persisted with the proposal.
/// </param>
public sealed record ScheduleOptimisationResult(
    string ModelName,
    string Objective,
    IReadOnlyList<ProposedTaskAssignment> Assignments,
    IReadOnlyList<string> ConstraintsHonoured,
    IReadOnlyList<string> ConstraintsRelaxed,
    string ExpectedGain);

public interface IScheduleOptimisationModel
{
    Task<ScheduleOptimisationResult> OptimiseAsync(
        ScheduleOptimisationInput input,
        CancellationToken cancellationToken = default);
}

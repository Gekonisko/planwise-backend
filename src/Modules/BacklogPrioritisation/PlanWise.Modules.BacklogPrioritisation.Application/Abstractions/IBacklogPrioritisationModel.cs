using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Abstractions;

// The prioritisation-model boundary, matching IRiskPredictionModel and ICostEstimationModel:
// Application depends only on this interface, never on how an ordering is arrived at. Ranking a
// backlog is a judgement call with no ground truth to learn from — there is no "correct" order a
// model could be fitted against — so the likely replacement here is an LLM rather than a trained
// model, and the seam is shaped for either.

/// <param name="BacklogTasks">
/// Already filtered to the backlog and in the user's current order. Scope is the caller's decision,
/// not the model's: this screen orders the product backlog specifically, and current positions are
/// taken from this list's order so a model cannot silently redefine what it is reordering.
/// </param>
/// <param name="RiskScores">
/// Slip probability per task from RiskPrediction, passed in rather than fetched by the model so a
/// prioritisation stays a pure function of its input and can be replayed. Tasks absent from the map
/// have no forecast yet; an implementation decides how to treat that (the scorecard uses 0.5).
/// </param>
public sealed record PrioritisationInput(
    Guid ProjectId,
    IReadOnlyList<TaskInsightSummary> BacklogTasks,
    IReadOnlyDictionary<Guid, decimal> RiskScores);

/// <param name="Reason">
/// Why this item sits where it does, shown in the explain drawer. This is the only explanation
/// channel a prioritisation has, so an implementation should make it specific to the task.
/// </param>
public sealed record PrioritisedTask(
    Guid TaskId,
    string TaskKey,
    decimal ValueScore,
    decimal DependencyScore,
    decimal ComplexityScore,
    decimal RiskScore,
    string Reason);

/// <param name="Ordered">
/// The proposed backlog order — position is the index, most important first. A model may return
/// fewer items than it was given (dropping ones it can't judge); the caller keeps whatever comes
/// back and does not re-add the rest.
/// </param>
public sealed record PrioritisationResult(IReadOnlyList<PrioritisedTask> Ordered);

public interface IBacklogPrioritisationModel
{
    /// <summary>Identifies which model produced a run, and is persisted with it.</summary>
    string ModelName { get; }

    Task<PrioritisationResult> PrioritiseAsync(PrioritisationInput input, CancellationToken cancellationToken = default);
}

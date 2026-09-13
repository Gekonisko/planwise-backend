using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities;

// The baseline model: a deterministic weighted scorecard (PriorityScorer), not a trained or
// generative one. Kept once something smarter exists — it is the control condition any replacement
// has to beat, and it is the only implementation that runs with no external dependency at all.
//
// Synchronous work behind an async interface (hence Task.FromResult); the seam is async because the
// implementation likely to replace this one is an LLM call.
public sealed class WeightedScorecardPrioritisationModel : IBacklogPrioritisationModel
{
    public string ModelName => "WeightedScorecard v1";

    public Task<PrioritisationResult> PrioritiseAsync(
        PrioritisationInput input,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PriorityScorer.ScoredTask> scored = PriorityScorer.Score(input.BacklogTasks, input.RiskScores);

        var ordered = scored
            .Select(item => new PrioritisedTask(
                item.Task.TaskId,
                item.Task.Key,
                item.ValueScore,
                item.DependencyScore,
                item.ComplexityScore,
                item.RiskScore,
                item.Reason))
            .ToList();

        return Task.FromResult(new PrioritisationResult(ordered));
    }
}

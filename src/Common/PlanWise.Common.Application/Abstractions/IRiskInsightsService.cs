namespace PlanWise.Common.Application.Abstractions;

// Owned by RiskPrediction. BacklogPrioritisation's "risk" component score consumes the latest
// per-task slip probability without depending on RiskPrediction's namespace — the score is empty
// (not missing) if a project's backlog has never been through a risk-forecast run, and callers treat
// an absent entry as a neutral 0.5 rather than an error.
public interface IRiskInsightsService
{
    Task<IReadOnlyDictionary<Guid, decimal>> GetLatestRiskScoresAsync(Guid projectId, CancellationToken cancellationToken = default);
}

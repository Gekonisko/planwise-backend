using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Modules.RiskPrediction.Application.Risks;

// Converts a team into a delivery rate in points per day.
//
// This exists because ProjectMemberSummary.Capacity is an FTE *fraction* — 1.0 is full-time, 0.5 is
// half-time — and summing it yields full-time-equivalents, not story points. Feeding that sum to
// SprintForecaster as if it were a points budget made a three-person team worth 3 points per
// sprint, so every sprint forecast came back near-certain to fail with a delivery date months out.
//
// FTE only becomes points through a rate, and the only honest source for that rate is the project's
// own finished sprints. Where none exist the estimate falls back to a stated assumption, and says
// so — IsMeasured is what the caller surfaces to the reader, because a rate observed over six
// sprints and a rate assumed out of thin air should not be presented the same way.
internal static class VelocityEstimator
{
    /// <summary>
    /// Points one full-time member is assumed to deliver per calendar day when the project has no
    /// completed sprint to measure. Roughly eight points per fortnight — a common planning rule of
    /// thumb, and deliberately conservative. It is an assumption, never a measurement.
    /// </summary>
    private const decimal AssumedPointsPerFtePerDay = 0.6m;

    /// <param name="IsMeasured">
    /// False when no completed sprint delivered any points and the rate came from
    /// <see cref="AssumedPointsPerFtePerDay"/> instead.
    /// </param>
    public sealed record Estimate(decimal PointsPerDay, int ObservedSprints, bool IsMeasured);

    public static Estimate ForProject(
        IReadOnlyList<SprintInsightSummary> sprints,
        IReadOnlyList<TaskInsightSummary> tasks,
        decimal teamFullTimeEquivalents)
    {
        var deliveredBySprint = tasks
            .Where(task => task.SprintId is not null && task.Status == DoneStatus)
            .GroupBy(task => task.SprintId!.Value)
            .ToDictionary(group => group.Key, group => (decimal)group.Sum(task => task.Points ?? 0));

        // Averaged per day rather than per sprint so that sprints of different lengths compare.
        var observedRates = sprints
            .Where(sprint => sprint.State == CompletedState)
            .Select(sprint => new
            {
                Points = deliveredBySprint.GetValueOrDefault(sprint.SprintId),
                Days = Math.Max(1, sprint.EndDate.DayNumber - sprint.StartDate.DayNumber),
            })
            .Where(sprint => sprint.Points > 0)
            .Select(sprint => sprint.Points / sprint.Days)
            .ToList();

        return observedRates.Count > 0
            ? new Estimate(observedRates.Average(), observedRates.Count, IsMeasured: true)
            : new Estimate(teamFullTimeEquivalents * AssumedPointsPerFtePerDay, 0, IsMeasured: false);
    }

    private const string DoneStatus = "Done";
    private const string CompletedState = "Completed";
}

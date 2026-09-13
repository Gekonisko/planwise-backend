using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.RiskPrediction.Application.Risks;

namespace PlanWise.Modules.RiskPrediction.Application.Tests;

/// <summary>
/// Guards the units bug that made every sprint forecast hopeless.
///
/// ProjectMemberSummary.Capacity is an FTE fraction, so a three-person team sums to 3.0. That sum
/// was previously handed to SprintForecaster as a story-point budget, which valued the whole team at
/// three points per sprint: a 20-point sprint then needed months, and the completion probability
/// came out near zero for every project regardless of how it was really going.
///
/// The failure was entirely plausible-looking from the outside — a low number, not an exception —
/// which is why the arithmetic is pinned here rather than left to be noticed again.
/// </summary>
public sealed class VelocityEstimatorTests
{
    private static readonly DateOnly SprintStart = new(2026, 1, 1);

    [Fact]
    public void Velocity_is_measured_from_completed_sprints_when_the_project_has_any()
    {
        // Two completed sprints, 28 and 42 points over 14 days each: 2 and 3 points per day.
        SprintInsightSummary[] sprints =
        [
            Sprint("s1", SprintStart, 14, "Completed"),
            Sprint("s2", SprintStart.AddDays(14), 14, "Completed"),
        ];

        var tasks = Done(sprints[0], points: 28).Concat(Done(sprints[1], points: 42)).ToList();

        VelocityEstimator.Estimate estimate = VelocityEstimator.ForProject(sprints, tasks, teamFullTimeEquivalents: 3m);

        Assert.True(estimate.IsMeasured);
        Assert.Equal(2, estimate.ObservedSprints);
        Assert.Equal(2.5m, estimate.PointsPerDay);
    }

    [Fact]
    public void Unfinished_work_in_a_completed_sprint_does_not_count_as_delivered()
    {
        SprintInsightSummary[] sprints = [Sprint("s1", SprintStart, 14, "Completed")];
        var tasks = Done(sprints[0], points: 14)
            .Concat([Task(sprints[0], points: 100, status: "InProgress")])
            .ToList();

        VelocityEstimator.Estimate estimate = VelocityEstimator.ForProject(sprints, tasks, 3m);

        Assert.Equal(1m, estimate.PointsPerDay);
    }

    [Fact]
    public void An_active_sprint_is_not_evidence_of_velocity_yet()
    {
        // Counting the in-flight sprint would drag the rate down purely because it is not finished.
        SprintInsightSummary[] sprints = [Sprint("s1", SprintStart, 14, "Active")];

        VelocityEstimator.Estimate estimate =
            VelocityEstimator.ForProject(sprints, Done(sprints[0], points: 7).ToList(), 2m);

        Assert.False(estimate.IsMeasured);
        Assert.Equal(0, estimate.ObservedSprints);
    }

    [Fact]
    public void Without_history_the_fallback_scales_with_the_team_and_is_flagged_as_assumed()
    {
        VelocityEstimator.Estimate estimate = VelocityEstimator.ForProject([], [], teamFullTimeEquivalents: 2.5m);

        Assert.False(estimate.IsMeasured);
        Assert.Equal(1.5m, estimate.PointsPerDay);   // 2.5 FTE x 0.6 points per FTE per day
    }

    [Fact]
    public void A_normal_sprint_is_not_forecast_as_doomed()
    {
        // The regression the user actually reported: a three-person team on a 20-point fortnight
        // used to come back at roughly 1% likely to finish, with delivery months away. Under the old
        // arithmetic the whole team was worth 3 points per sprint.
        SprintInsightSummary[] sprints = [Sprint("s1", SprintStart, 14, "Active")];
        TaskInsightSummary[] tasks = [Task(sprints[0], points: 20, status: "Todo")];

        VelocityEstimator.Estimate velocity = VelocityEstimator.ForProject(sprints, tasks, teamFullTimeEquivalents: 3m);
        SprintForecaster.ForecastResult forecast =
            SprintForecaster.Forecast(sprints[0], tasks, velocity.PointsPerDay, SprintStart);

        Assert.True(forecast.CompletionProbability > 0.8m,
            $"a 20-point sprint for three people should look achievable, got {forecast.CompletionProbability:P0}");
        Assert.True(forecast.P50DeliveryDate <= sprints[0].EndDate.AddDays(1),
            $"p50 delivery {forecast.P50DeliveryDate} should land near the sprint end {sprints[0].EndDate}");
    }

    private static SprintInsightSummary Sprint(string name, DateOnly start, int lengthDays, string state) =>
        new(Guid.NewGuid(), Guid.Empty, name, start, start.AddDays(lengthDays), state);

    private static IEnumerable<TaskInsightSummary> Done(SprintInsightSummary sprint, int points) =>
        [Task(sprint, points, "Done")];

    private static TaskInsightSummary Task(SprintInsightSummary sprint, int points, string status) =>
        new(Guid.NewGuid(), Guid.Empty, "PW-1", "Task", status, "Medium", points, null, null, null,
            sprint.SprintId, 0m, 0, 0, [], 0, null);
}

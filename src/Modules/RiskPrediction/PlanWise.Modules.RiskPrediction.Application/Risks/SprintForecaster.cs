using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Modules.RiskPrediction.Application.Risks;

// Projects a sprint forward at a constant daily rate. The rate itself comes from VelocityEstimator
// — measured from the project's own completed sprints where there are any — and is passed in rather
// than derived here, so this stays a pure projection and the question of "how fast is this team"
// lives in exactly one place.
//
// p90 is a fixed pessimistic multiplier on the p50 gap, not a real distribution.
internal static class SprintForecaster
{
    private const double P90Multiplier = 1.4;

    public sealed record ForecastResult(decimal CompletionProbability, decimal ExpectedPoints, DateOnly P50DeliveryDate, DateOnly P90DeliveryDate);

    public static ForecastResult Forecast(
        SprintInsightSummary sprint,
        IReadOnlyList<TaskInsightSummary> sprintTasks,
        decimal dailyVelocityPoints,
        DateOnly today)
    {
        decimal committed = sprintTasks.Sum(task => task.Points ?? 0);
        decimal completed = sprintTasks.Where(task => task.Status == "Done").Sum(task => task.Points ?? 0);
        decimal remaining = Math.Max(0m, committed - completed);

        decimal dailyVelocity = Math.Max(0m, dailyVelocityPoints);
        int daysRemaining = Math.Max(0, sprint.EndDate.DayNumber - today.DayNumber);

        decimal expectedPoints = committed == 0
            ? 0m
            : Math.Min(committed, completed + dailyVelocity * daysRemaining);
        decimal completionProbability = committed == 0 ? 1m : Math.Clamp(expectedPoints / committed, 0m, 1m);

        DateOnly p50 = ProjectDeliveryDate(today, remaining, dailyVelocity, 1.0, sprint.EndDate);
        DateOnly p90 = ProjectDeliveryDate(today, remaining, dailyVelocity, P90Multiplier, sprint.EndDate.AddDays(7));
        if (p90 < p50)
        {
            p90 = p50;
        }

        return new ForecastResult(completionProbability, expectedPoints, p50, p90);
    }

    private static DateOnly ProjectDeliveryDate(DateOnly today, decimal remainingPoints, decimal dailyVelocity, double multiplier, DateOnly fallback)
    {
        if (remainingPoints <= 0)
        {
            return today;
        }

        if (dailyVelocity <= 0)
        {
            return fallback;
        }

        double daysNeeded = (double)(remainingPoints / dailyVelocity) * multiplier;
        return today.AddDays((int)Math.Ceiling(daysNeeded));
    }
}

namespace PlanWise.Common.Infrastructure.Jobs;

public sealed class AsyncJobRunnerOptions
{
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(2);

    public int BatchSize { get; init; } = 10;
}

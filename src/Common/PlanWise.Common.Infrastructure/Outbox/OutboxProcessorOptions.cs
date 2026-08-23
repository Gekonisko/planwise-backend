namespace PlanWise.Common.Infrastructure.Outbox;

public sealed class OutboxProcessorOptions
{
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);

    public int BatchSize { get; init; } = 20;
}

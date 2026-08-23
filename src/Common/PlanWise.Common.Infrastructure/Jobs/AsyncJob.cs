using PlanWise.Common.Application.Abstractions;

namespace PlanWise.Common.Infrastructure.Jobs;

public sealed class AsyncJob
{
    public Guid Id { get; init; }

    public string JobType { get; init; }

    public Guid ProjectId { get; init; }

    public AsyncJobStatus Status { get; private set; } = AsyncJobStatus.Queued;

    public string? ResultLocation { get; private set; }

    public string? Error { get; private set; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; private set; }

    public void MarkRunning() => Status = AsyncJobStatus.Running;

    public void MarkSucceeded(string resultLocation, DateTime completedAtUtc)
    {
        Status = AsyncJobStatus.Succeeded;
        ResultLocation = resultLocation;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkFailed(string error, DateTime completedAtUtc)
    {
        Status = AsyncJobStatus.Failed;
        Error = error;
        CompletedAtUtc = completedAtUtc;
    }
}

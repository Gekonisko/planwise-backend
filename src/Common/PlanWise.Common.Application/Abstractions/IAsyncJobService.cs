namespace PlanWise.Common.Application.Abstractions;

public enum AsyncJobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed
}

public sealed record JobEnqueuedResponse(Guid JobId);

public sealed record AsyncJobResponse(
    Guid Id,
    Guid ProjectId,
    string JobType,
    AsyncJobStatus Status,
    string? ResultLocation,
    string? Error,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

// Consumer-facing half of the shared "intelligence endpoint" job contract: a module that starts a
// long-running (or, today, fast-but-still-async-shaped) run calls EnqueueAsync and returns the job id
// as 202 Accepted; GET /jobs/{id} (hosted centrally, see JobEndpoints) reads status via GetAsync. The
// other half, IAsyncJobHandler, is how a module registers the actual work the runner executes.
public interface IAsyncJobService
{
    Task<Guid> EnqueueAsync(string jobType, Guid projectId, CancellationToken cancellationToken = default);

    Task<AsyncJobResponse?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);
}

// Implemented by whichever module owns a given job type (e.g. Scheduling for "ScheduleOptimisation")
// and registered as IAsyncJobHandler in that module's DI so the shared AsyncJobRunner can find it by
// JobType. ExecuteAsync does the actual work and is responsible for persisting its own result
// somewhere addressable, returning that address as ResultLocation; throwing marks the job Failed.
public interface IAsyncJobHandler
{
    string JobType { get; }

    Task<string> ExecuteAsync(Guid jobId, Guid projectId, CancellationToken cancellationToken);
}

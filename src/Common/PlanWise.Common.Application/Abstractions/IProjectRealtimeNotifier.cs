namespace PlanWise.Common.Application.Abstractions;

// Owned by Common (the SignalR hub is cross-cutting infrastructure, not any one module's concern).
// Implemented over IHubContext<ProjectHub> in Common.Infrastructure; any module pushes a realtime
// event to everyone watching a project's board by calling this, without referencing SignalR types
// itself.
public interface IProjectRealtimeNotifier
{
    Task TaskMovedAsync(
        Guid projectId, Guid taskId, string taskKey, string fromStatus, string toStatus,
        CancellationToken cancellationToken = default);

    Task TaskUpdatedAsync(
        Guid projectId, Guid taskId, string taskKey,
        CancellationToken cancellationToken = default);

    Task JobFinishedAsync(
        Guid projectId, Guid jobId, string jobType, string status,
        CancellationToken cancellationToken = default);
}

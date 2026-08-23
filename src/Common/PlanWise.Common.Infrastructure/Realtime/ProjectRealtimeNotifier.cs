using Microsoft.AspNetCore.SignalR;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Presentation.Hubs;

namespace PlanWise.Common.Infrastructure.Realtime;

internal sealed class ProjectRealtimeNotifier(IHubContext<ProjectHub> hubContext) : IProjectRealtimeNotifier
{
    public Task TaskMovedAsync(
        Guid projectId, Guid taskId, string taskKey, string fromStatus, string toStatus,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(ProjectHub.GroupName(projectId)).SendAsync(
            "taskMoved", new { taskId, taskKey, fromStatus, toStatus }, cancellationToken);

    public Task TaskUpdatedAsync(
        Guid projectId, Guid taskId, string taskKey,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(ProjectHub.GroupName(projectId)).SendAsync(
            "taskUpdated", new { taskId, taskKey }, cancellationToken);

    public Task JobFinishedAsync(
        Guid projectId, Guid jobId, string jobType, string status,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(ProjectHub.GroupName(projectId)).SendAsync(
            "jobFinished", new { jobId, jobType, status }, cancellationToken);
}

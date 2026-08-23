using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Infrastructure.Database;

namespace PlanWise.Common.Infrastructure.Jobs;

// Shared runner for every "intelligence endpoint" job across all modules: polls common.async_jobs for
// Queued rows and dispatches each to the IAsyncJobHandler registered (by any module, via plain DI)
// for that row's JobType. Today's only handler (Scheduling's optimiser) is a fast deterministic
// heuristic, not a real long-running model, so in practice a job usually reaches Succeeded well
// before a client's first poll — the async/poll contract is honoured regardless, so slower handlers
// (a real ML/LLM call) can be dropped in later without changing the API shape.
internal sealed class AsyncJobRunner(
    IServiceScopeFactory scopeFactory,
    AsyncJobRunnerOptions options,
    ILogger<AsyncJobRunner> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.PollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessQueuedJobsAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Unhandled error while processing async jobs");
            }
        }
    }

    private async Task ProcessQueuedJobsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        CommonDbContext dbContext = scope.ServiceProvider.GetRequiredService<CommonDbContext>();
        IDateTimeProvider dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        List<AsyncJob> jobs = await dbContext.AsyncJobs
            .Where(job => job.Status == AsyncJobStatus.Queued)
            .OrderBy(job => job.CreatedAtUtc)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        if (jobs.Count == 0)
        {
            return;
        }

        var handlersByType = scope.ServiceProvider.GetServices<IAsyncJobHandler>().ToDictionary(handler => handler.JobType);

        foreach (AsyncJob job in jobs)
        {
            await ProcessJobAsync(job, handlersByType, dateTimeProvider, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessJobAsync(
        AsyncJob job,
        Dictionary<string, IAsyncJobHandler> handlersByType,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken)
    {
        job.MarkRunning();

        if (!handlersByType.TryGetValue(job.JobType, out IAsyncJobHandler? handler))
        {
            job.MarkFailed($"No handler registered for job type '{job.JobType}'", dateTimeProvider.UtcNow);
            return;
        }

        try
        {
            string resultLocation = await handler.ExecuteAsync(job.Id, job.ProjectId, cancellationToken);
            job.MarkSucceeded(resultLocation, dateTimeProvider.UtcNow);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error processing async job {JobId} of type {JobType}", job.Id, job.JobType);
            job.MarkFailed(exception.Message, dateTimeProvider.UtcNow);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Infrastructure.Database;

namespace PlanWise.Common.Infrastructure.Jobs;

internal sealed class AsyncJobService(CommonDbContext dbContext, IDateTimeProvider dateTimeProvider) : IAsyncJobService
{
    public async Task<Guid> EnqueueAsync(string jobType, Guid projectId, CancellationToken cancellationToken = default)
    {
        var job = new AsyncJob
        {
            Id = Guid.NewGuid(),
            JobType = jobType,
            ProjectId = projectId,
            CreatedAtUtc = dateTimeProvider.UtcNow
        };

        dbContext.AsyncJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        return job.Id;
    }

    public async Task<AsyncJobResponse?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        AsyncJob? job = await dbContext.AsyncJobs.SingleOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        return job is null
            ? null
            : new AsyncJobResponse(job.Id, job.ProjectId, job.JobType, job.Status, job.ResultLocation, job.Error, job.CreatedAtUtc, job.CompletedAtUtc);
    }
}

using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;

namespace PlanWise.Common.Application.Jobs.GetJob;

internal sealed class GetJobQueryHandler(
    IAsyncJobService asyncJobService,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetJobQuery, AsyncJobResponse>
{
    public async Task<Result<AsyncJobResponse>> Handle(GetJobQuery request, CancellationToken cancellationToken)
    {
        AsyncJobResponse? job = await asyncJobService.GetAsync(request.JobId, cancellationToken);
        if (job is null)
        {
            return Result.Failure<AsyncJobResponse>(JobErrors.NotFound(request.JobId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(job.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<AsyncJobResponse>(JobErrors.NotFound(request.JobId));
        }

        return Result.Success(job);
    }
}

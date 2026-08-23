using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Authentication;
using PlanWise.Modules.RiskPrediction.Domain;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.RunForecast;

internal sealed class RunForecastCommandHandler(
    IProjectAccessService projectAccessService,
    IAsyncJobService asyncJobService,
    IUserContext userContext)
    : ICommandHandler<RunForecastCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RunForecastCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<Guid>(RiskErrors.ProjectNotFound(request.ProjectId));
        }

        Guid jobId = await asyncJobService.EnqueueAsync("RiskForecast", request.ProjectId, cancellationToken);
        return Result.Success(jobId);
    }
}

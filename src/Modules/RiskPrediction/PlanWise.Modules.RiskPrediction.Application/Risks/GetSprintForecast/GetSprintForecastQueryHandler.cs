using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Authentication;
using PlanWise.Modules.RiskPrediction.Domain;
using PlanWise.Modules.RiskPrediction.Domain.Risks;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.GetSprintForecast;

internal sealed class GetSprintForecastQueryHandler(
    ISprintForecastRepository forecastRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetSprintForecastQuery, SprintForecastResponse>
{
    public async Task<Result<SprintForecastResponse>> Handle(GetSprintForecastQuery request, CancellationToken cancellationToken)
    {
        SprintForecast? forecast = await forecastRepository.GetLatestForSprintAsync(request.SprintId, cancellationToken);
        if (forecast is null)
        {
            return Result.Failure<SprintForecastResponse>(RiskErrors.NoForecastForSprint(request.SprintId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(forecast.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<SprintForecastResponse>(RiskErrors.NoForecastForSprint(request.SprintId));
        }

        return Result.Success(RiskMappings.ToResponse(forecast));
    }
}

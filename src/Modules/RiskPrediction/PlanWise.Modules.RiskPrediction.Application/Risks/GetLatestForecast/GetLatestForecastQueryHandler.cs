using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Authentication;
using PlanWise.Modules.RiskPrediction.Domain;
using PlanWise.Modules.RiskPrediction.Domain.Risks;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.GetLatestForecast;

internal sealed class GetLatestForecastQueryHandler(
    IRiskAssessmentRunRepository runRepository,
    ITaskRiskAssessmentRepository taskRiskAssessmentRepository,
    ISprintForecastRepository sprintForecastRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetLatestForecastQuery, LatestForecastResponse>
{
    public async Task<Result<LatestForecastResponse>> Handle(GetLatestForecastQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<LatestForecastResponse>(RiskErrors.ProjectNotFound(request.ProjectId));
        }

        RiskAssessmentRun? run = await runRepository.GetLatestForProjectAsync(request.ProjectId, cancellationToken);
        if (run is null)
        {
            return Result.Failure<LatestForecastResponse>(RiskErrors.NoRunForProject(request.ProjectId));
        }

        IReadOnlyList<TaskRiskAssessment> assessments = await taskRiskAssessmentRepository.GetForRunAsync(run.Id, excludeDismissed: false, cancellationToken);
        IReadOnlyList<SprintForecast> forecasts = await sprintForecastRepository.GetForRunAsync(run.Id, cancellationToken);

        return Result.Success(new LatestForecastResponse(
            run.Id,
            run.ModelVersion,
            run.TrainingWindowDays,
            assessments.Count,
            forecasts.Select(forecast => forecast.SprintId).ToList(),
            run.CreatedAtUtc));
    }
}

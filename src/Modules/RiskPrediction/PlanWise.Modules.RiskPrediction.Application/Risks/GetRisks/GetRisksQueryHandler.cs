using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Authentication;
using PlanWise.Modules.RiskPrediction.Domain;
using PlanWise.Modules.RiskPrediction.Domain.Risks;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.GetRisks;

internal sealed class GetRisksQueryHandler(
    IRiskAssessmentRunRepository runRepository,
    ITaskRiskAssessmentRepository taskRiskAssessmentRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetRisksQuery, IReadOnlyList<TaskRiskResponse>>
{
    public async Task<Result<IReadOnlyList<TaskRiskResponse>>> Handle(GetRisksQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<TaskRiskResponse>>(RiskErrors.ProjectNotFound(request.ProjectId));
        }

        RiskAssessmentRun? run = await runRepository.GetLatestForProjectAsync(request.ProjectId, cancellationToken);
        if (run is null)
        {
            return Result.Success<IReadOnlyList<TaskRiskResponse>>([]);
        }

        IReadOnlyList<TaskRiskAssessment> assessments = await taskRiskAssessmentRepository.GetForRunAsync(run.Id, excludeDismissed: true, cancellationToken);
        return Result.Success<IReadOnlyList<TaskRiskResponse>>(assessments.Select(RiskMappings.ToResponse).ToList());
    }
}

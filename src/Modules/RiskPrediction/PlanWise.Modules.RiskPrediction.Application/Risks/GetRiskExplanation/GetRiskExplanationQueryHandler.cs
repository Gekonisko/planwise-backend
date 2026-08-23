using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Authentication;
using PlanWise.Modules.RiskPrediction.Domain;
using PlanWise.Modules.RiskPrediction.Domain.Risks;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.GetRiskExplanation;

internal sealed class GetRiskExplanationQueryHandler(
    ITaskRiskAssessmentRepository taskRiskAssessmentRepository,
    IRiskAssessmentRunRepository runRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetRiskExplanationQuery, RiskExplanationResponse>
{
    public async Task<Result<RiskExplanationResponse>> Handle(GetRiskExplanationQuery request, CancellationToken cancellationToken)
    {
        TaskRiskAssessment? assessment = await taskRiskAssessmentRepository.GetAsync(request.Id, cancellationToken);
        if (assessment is null)
        {
            return Result.Failure<RiskExplanationResponse>(RiskErrors.AssessmentNotFound(request.Id));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(assessment.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<RiskExplanationResponse>(RiskErrors.AssessmentNotFound(request.Id));
        }

        RiskAssessmentRun? run = await runRepository.GetAsync(assessment.RunId, cancellationToken);
        if (run is null)
        {
            return Result.Failure<RiskExplanationResponse>(RiskErrors.AssessmentNotFound(request.Id));
        }

        return Result.Success(RiskMappings.ToExplanationResponse(assessment, run));
    }
}

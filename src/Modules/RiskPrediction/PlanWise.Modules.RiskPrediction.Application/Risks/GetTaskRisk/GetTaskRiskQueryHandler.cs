using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Authentication;
using PlanWise.Modules.RiskPrediction.Domain;
using PlanWise.Modules.RiskPrediction.Domain.Risks;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.GetTaskRisk;

internal sealed class GetTaskRiskQueryHandler(
    ITaskRiskAssessmentRepository taskRiskAssessmentRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetTaskRiskQuery, TaskRiskResponse>
{
    public async Task<Result<TaskRiskResponse>> Handle(GetTaskRiskQuery request, CancellationToken cancellationToken)
    {
        TaskRiskAssessment? assessment = await taskRiskAssessmentRepository.GetLatestForTaskAsync(request.TaskId, cancellationToken);
        if (assessment is null)
        {
            return Result.Failure<TaskRiskResponse>(RiskErrors.NoAssessmentForTask(request.TaskId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(assessment.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<TaskRiskResponse>(RiskErrors.NoAssessmentForTask(request.TaskId));
        }

        return Result.Success(RiskMappings.ToResponse(assessment));
    }
}

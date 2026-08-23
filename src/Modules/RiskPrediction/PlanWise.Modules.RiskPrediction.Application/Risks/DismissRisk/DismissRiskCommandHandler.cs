using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Authentication;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Data;
using PlanWise.Modules.RiskPrediction.Domain;
using PlanWise.Modules.RiskPrediction.Domain.Risks;

namespace PlanWise.Modules.RiskPrediction.Application.Risks.DismissRisk;

internal sealed class DismissRiskCommandHandler(
    ITaskRiskAssessmentRepository taskRiskAssessmentRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DismissRiskCommand>
{
    public async Task<Result> Handle(DismissRiskCommand request, CancellationToken cancellationToken)
    {
        TaskRiskAssessment? assessment = await taskRiskAssessmentRepository.GetAsync(request.Id, cancellationToken);
        if (assessment is null)
        {
            return Result.Failure(RiskErrors.AssessmentNotFound(request.Id));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(assessment.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure(RiskErrors.AssessmentNotFound(request.Id));
        }

        assessment.Dismiss(request.Reason, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

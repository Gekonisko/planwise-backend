using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Authentication;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Data;
using PlanWise.Modules.CostEstimation.Domain;
using PlanWise.Modules.CostEstimation.Domain.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.Reductions.UnapplyReduction;

internal sealed class UnapplyReductionCommandHandler(
    ICostEstimateRunRepository runRepository,
    IAppliedReductionRepository appliedReductionRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<UnapplyReductionCommand, ReductionsResponse>
{
    public async Task<Result<ReductionsResponse>> Handle(UnapplyReductionCommand request, CancellationToken cancellationToken)
    {
        CostEstimateRun? run = await runRepository.GetAsync(request.RunId, cancellationToken);
        if (run is null)
        {
            return Result.Failure<ReductionsResponse>(CostEstimateErrors.RunNotFound(request.RunId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(run.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<ReductionsResponse>(CostEstimateErrors.RunNotFound(request.RunId));
        }

        AppliedReduction? existing = await appliedReductionRepository.GetAsync(run.Id, request.ReductionId, cancellationToken);
        if (existing is not null)
        {
            appliedReductionRepository.Remove(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        IReadOnlyList<AppliedReduction> applied = await appliedReductionRepository.GetForRunAsync(run.Id, cancellationToken);
        return Result.Success(ReductionMappings.BuildResponse(run, applied));
    }
}

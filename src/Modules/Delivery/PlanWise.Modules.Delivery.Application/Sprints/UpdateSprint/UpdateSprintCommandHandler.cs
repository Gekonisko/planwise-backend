using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints.UpdateSprint;

internal sealed class UpdateSprintCommandHandler(
    ISprintRepository sprintRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<UpdateSprintCommand, SprintResponse>
{
    public async Task<Result<SprintResponse>> Handle(UpdateSprintCommand request, CancellationToken cancellationToken)
    {
        Sprint? sprint = await sprintRepository.GetAsync(request.SprintId, cancellationToken);
        if (sprint is null)
        {
            return Result.Failure<SprintResponse>(SprintErrors.NotFound(request.SprintId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(sprint.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<SprintResponse>(SprintErrors.NotFound(request.SprintId));
        }

        sprint.Update(request.Name?.Trim(), request.Goal?.Trim(), request.StartDate, request.EndDate);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SprintMappings.ToResponse(sprint));
    }
}

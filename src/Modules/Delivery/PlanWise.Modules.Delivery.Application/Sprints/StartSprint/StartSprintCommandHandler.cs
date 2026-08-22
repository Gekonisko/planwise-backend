using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints.StartSprint;

internal sealed class StartSprintCommandHandler(
    ISprintRepository sprintRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<StartSprintCommand, SprintResponse>
{
    public async Task<Result<SprintResponse>> Handle(StartSprintCommand request, CancellationToken cancellationToken)
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

        if (await sprintRepository.HasActiveSprintAsync(sprint.ProjectId, cancellationToken))
        {
            return Result.Failure<SprintResponse>(SprintErrors.AlreadyActive(sprint.ProjectId));
        }

        Result startResult = sprint.Start();
        if (startResult.IsFailure)
        {
            return Result.Failure<SprintResponse>(startResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(SprintMappings.ToResponse(sprint));
    }
}

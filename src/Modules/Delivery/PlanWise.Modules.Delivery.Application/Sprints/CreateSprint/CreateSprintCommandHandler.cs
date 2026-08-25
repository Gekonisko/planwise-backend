using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Sprints;

namespace PlanWise.Modules.Delivery.Application.Sprints.CreateSprint;

internal sealed class CreateSprintCommandHandler(
    ISprintRepository sprintRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<CreateSprintCommand, SprintResponse>
{
    public async Task<Result<SprintResponse>> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<SprintResponse>(SprintErrors.ProjectNotFound(request.ProjectId));
        }

        var sprint = Sprint.Create(request.ProjectId, request.Name.Trim(), request.Goal?.Trim(), request.StartDate, request.EndDate);
        sprintRepository.Add(sprint);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SprintMappings.ToResponse(sprint, []));
    }
}

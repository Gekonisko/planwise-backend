using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Domain.Milestones;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Milestones.CreateMilestone;

internal sealed class CreateMilestoneCommandHandler(
    IMilestoneRepository milestoneRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<CreateMilestoneCommand, MilestoneResponse>
{
    public async Task<Result<MilestoneResponse>> Handle(CreateMilestoneCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<MilestoneResponse>(ScheduleErrors.ProjectNotFound(request.ProjectId));
        }

        var milestone = Milestone.Create(request.ProjectId, request.Name.Trim(), request.DueDate);
        milestoneRepository.Add(milestone);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        return Result.Success(MilestoneMappings.ToResponse(milestone, today));
    }
}

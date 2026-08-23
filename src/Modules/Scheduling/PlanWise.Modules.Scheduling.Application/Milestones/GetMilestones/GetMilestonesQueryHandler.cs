using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Domain.Milestones;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Milestones.GetMilestones;

internal sealed class GetMilestonesQueryHandler(
    IMilestoneRepository milestoneRepository,
    IProjectAccessService projectAccessService,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : IQueryHandler<GetMilestonesQuery, IReadOnlyList<MilestoneResponse>>
{
    public async Task<Result<IReadOnlyList<MilestoneResponse>>> Handle(GetMilestonesQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<MilestoneResponse>>(ScheduleErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<Milestone> milestones = await milestoneRepository.GetByProjectAsync(request.ProjectId, cancellationToken);
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        IReadOnlyList<MilestoneResponse> responses = milestones
            .OrderBy(milestone => milestone.DueDate)
            .Select(milestone => MilestoneMappings.ToResponse(milestone, today))
            .ToList();

        return Result.Success(responses);
    }
}

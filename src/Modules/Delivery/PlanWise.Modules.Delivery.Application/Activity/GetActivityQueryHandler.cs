using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Domain.Activity;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Activity;

internal sealed class GetActivityQueryHandler(
    IActivityLogRepository activityLogRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetActivityQuery, IReadOnlyList<ActivityEntryResponse>>
{
    public async Task<Result<IReadOnlyList<ActivityEntryResponse>>> Handle(GetActivityQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<ActivityEntryResponse>>(TaskErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<ActivityLogEntry> entries = await activityLogRepository.GetByProjectAsync(
            request.ProjectId, request.Limit, request.Offset, cancellationToken);

        IReadOnlyList<ActivityEntryResponse> responses = entries
            .Select(entry => new ActivityEntryResponse(entry.Id, entry.ProjectId, entry.Description, entry.OccurredAtUtc))
            .ToList();

        return Result.Success(responses);
    }
}

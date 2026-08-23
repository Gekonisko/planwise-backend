using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Workload;

internal sealed class GetWorkloadQueryHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IProjectMembersService projectMembersService,
    IUserContext userContext)
    : IQueryHandler<GetWorkloadQuery, WorkloadResponse>
{
    public async Task<Result<WorkloadResponse>> Handle(GetWorkloadQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<WorkloadResponse>(TaskErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<ProjectMemberSummary> members = await projectMembersService.GetMembersAsync(request.ProjectId, cancellationToken);
        IReadOnlyList<ProjectTask> tasks = await taskRepository.GetByProjectAsync(
            request.ProjectId, request.SprintId, null, null, null, null, cancellationToken);

        var responses = members
            .Where(member => member.UserId is not null)
            .Select(member =>
            {
                int assignedPoints = tasks.Where(task => task.AssigneeId == member.UserId).Sum(task => task.Points ?? 0);
                return new MemberWorkloadResponse(member.UserId, member.Email, member.Capacity, assignedPoints);
            })
            .ToList();

        return Result.Success(new WorkloadResponse(responses));
    }
}

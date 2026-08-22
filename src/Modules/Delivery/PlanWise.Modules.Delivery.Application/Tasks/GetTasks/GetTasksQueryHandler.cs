using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.GetTasks;

internal sealed class GetTasksQueryHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetTasksQuery, IReadOnlyList<TaskResponse>>
{
    public async Task<Result<IReadOnlyList<TaskResponse>>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<TaskResponse>>(TaskErrors.ProjectNotFound(request.ProjectId));
        }

        ProjectTaskStatus? status = null;
        if (request.Status is not null && Enum.TryParse(request.Status, ignoreCase: true, out ProjectTaskStatus parsedStatus))
        {
            status = parsedStatus;
        }

        IReadOnlyList<ProjectTask> tasks = await taskRepository.GetByProjectAsync(
            request.ProjectId, request.SprintId, status, request.AssigneeId, request.Label, request.Q, cancellationToken);

        IReadOnlyList<TaskResponse> responses = tasks.Select(TaskMappings.ToResponse).ToList();
        return Result.Success(responses);
    }
}

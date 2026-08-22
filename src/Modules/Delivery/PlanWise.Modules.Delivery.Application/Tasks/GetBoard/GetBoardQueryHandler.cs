using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.GetBoard;

internal sealed class GetBoardQueryHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetBoardQuery, BoardResponse>
{
    private static readonly ProjectTaskStatus[] BoardColumns =
    [
        ProjectTaskStatus.Todo,
        ProjectTaskStatus.InProgress,
        ProjectTaskStatus.Done
    ];

    public async Task<Result<BoardResponse>> Handle(GetBoardQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<BoardResponse>(TaskErrors.ProjectNotFound(request.ProjectId));
        }

        var columns = new List<BoardColumnResponse>();
        foreach (ProjectTaskStatus status in BoardColumns)
        {
            IReadOnlyList<ProjectTask> tasks = await taskRepository.GetByStatusAsync(request.ProjectId, status, cancellationToken);
            IReadOnlyList<TaskResponse> taskResponses = tasks.Select(TaskMappings.ToResponse).ToList();
            int pointTotal = tasks.Sum(task => task.Points ?? 0);

            // WIP limits are not yet configurable per project/column, so this is always null until that setting exists.
            columns.Add(new BoardColumnResponse(status, null, pointTotal, taskResponses));
        }

        return Result.Success(new BoardResponse(columns));
    }
}

using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.MoveTask;

internal sealed class MoveTaskCommandHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<MoveTaskCommand, TaskResponse>
{
    public async Task<Result<TaskResponse>> Handle(MoveTaskCommand request, CancellationToken cancellationToken)
    {
        ProjectTask? task = await taskRepository.GetAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskResponse>(TaskErrors.NotFound(request.TaskId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(task.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<TaskResponse>(TaskErrors.NotFound(request.TaskId));
        }

        ProjectTaskStatus targetStatus = Enum.Parse<ProjectTaskStatus>(request.Status, ignoreCase: true);

        var columnTasks = (await taskRepository.GetByStatusAsync(task.ProjectId, targetStatus, cancellationToken))
            .Where(t => t.Id != task.Id)
            .ToList();

        int index = Math.Clamp(request.Index, 0, columnTasks.Count);
        decimal newRank = index switch
        {
            _ when columnTasks.Count == 0 => 1024m,
            0 => columnTasks[0].Rank - 1024m,
            _ when index >= columnTasks.Count => columnTasks[^1].Rank + 1024m,
            _ => (columnTasks[index - 1].Rank + columnTasks[index].Rank) / 2m
        };

        task.Move(targetStatus, newRank);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(TaskMappings.ToResponse(task));
    }
}

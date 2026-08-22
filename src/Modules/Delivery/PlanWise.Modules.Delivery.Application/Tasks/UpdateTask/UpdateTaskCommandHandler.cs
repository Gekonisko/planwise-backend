using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.UpdateTask;

internal sealed class UpdateTaskCommandHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<UpdateTaskCommand, TaskResponse>
{
    public async Task<Result<TaskResponse>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
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

        TaskPriority? priority = request.Priority is not null
            ? Enum.Parse<TaskPriority>(request.Priority, ignoreCase: true)
            : null;

        task.Update(
            request.Title?.Trim(),
            request.Description?.Trim(),
            priority,
            request.Points,
            request.AssigneeId,
            request.DueDate,
            request.SprintId);

        if (request.LabelIds is not null)
        {
            IReadOnlyList<TaskLabel> newLabels = task.ReplaceLabels(request.LabelIds);
            taskRepository.AddLabels(newLabels);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(TaskMappings.ToResponse(task));
    }
}

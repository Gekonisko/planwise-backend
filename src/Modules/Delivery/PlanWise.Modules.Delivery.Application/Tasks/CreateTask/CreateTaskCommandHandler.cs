using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.CreateTask;

internal sealed class CreateTaskCommandHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<CreateTaskCommand, TaskResponse>
{
    public async Task<Result<TaskResponse>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<TaskResponse>(TaskErrors.ProjectNotFound(request.ProjectId));
        }

        string? keyPrefix = await projectAccessService.GetKeyPrefixAsync(request.ProjectId, cancellationToken);
        if (keyPrefix is null)
        {
            return Result.Failure<TaskResponse>(TaskErrors.ProjectNotFound(request.ProjectId));
        }

        int number = await taskRepository.GetNextTaskNumberAsync(request.ProjectId, cancellationToken);
        string key = $"{keyPrefix}-{number}";

        decimal maxRank = await taskRepository.GetMaxRankAsync(request.ProjectId, ProjectTaskStatus.Backlog, cancellationToken);
        TaskPriority priority = Enum.Parse<TaskPriority>(request.Priority, ignoreCase: true);

        var task = ProjectTask.Create(
            request.ProjectId,
            key,
            request.Title.Trim(),
            request.Description?.Trim(),
            priority,
            request.Points,
            request.AssigneeId,
            request.DueDate,
            maxRank + 1024m);

        if (request.BusinessValue is not null)
        {
            task.SetBusinessValue(request.BusinessValue.Value);
        }

        if (request.LabelIds is { Count: > 0 })
        {
            task.ReplaceLabels(request.LabelIds);
        }

        taskRepository.Add(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(TaskMappings.ToResponse(task));
    }
}

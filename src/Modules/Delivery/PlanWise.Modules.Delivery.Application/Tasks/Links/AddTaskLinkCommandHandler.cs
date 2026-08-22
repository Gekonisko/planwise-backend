using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Links;

internal sealed class AddTaskLinkCommandHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<AddTaskLinkCommand, TaskLinkResponse>
{
    public async Task<Result<TaskLinkResponse>> Handle(AddTaskLinkCommand request, CancellationToken cancellationToken)
    {
        ProjectTask? task = await taskRepository.GetAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskLinkResponse>(TaskErrors.NotFound(request.TaskId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(task.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<TaskLinkResponse>(TaskErrors.NotFound(request.TaskId));
        }

        ProjectTask? linkedTask = await taskRepository.GetAsync(request.LinkedTaskId, cancellationToken);
        if (linkedTask is null || linkedTask.ProjectId != task.ProjectId)
        {
            return Result.Failure<TaskLinkResponse>(TaskErrors.NotFound(request.LinkedTaskId));
        }

        TaskLinkType type = Enum.Parse<TaskLinkType>(request.Type, ignoreCase: true);
        TaskLink link = task.AddLink(request.LinkedTaskId, type);
        taskRepository.AddLink(link);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(TaskMappings.ToResponse(link));
    }
}

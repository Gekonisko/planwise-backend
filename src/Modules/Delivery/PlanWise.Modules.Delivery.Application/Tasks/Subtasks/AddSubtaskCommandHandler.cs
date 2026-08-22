using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Subtasks;

internal sealed class AddSubtaskCommandHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<AddSubtaskCommand, SubtaskResponse>
{
    public async Task<Result<SubtaskResponse>> Handle(AddSubtaskCommand request, CancellationToken cancellationToken)
    {
        ProjectTask? task = await taskRepository.GetAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<SubtaskResponse>(TaskErrors.NotFound(request.TaskId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(task.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<SubtaskResponse>(TaskErrors.NotFound(request.TaskId));
        }

        Subtask subtask = task.AddSubtask(request.Title.Trim());
        taskRepository.AddSubtask(subtask);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(TaskMappings.ToResponse(subtask));
    }
}

using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Subtasks;

internal sealed class UpdateSubtaskCommandHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<UpdateSubtaskCommand, SubtaskResponse>
{
    public async Task<Result<SubtaskResponse>> Handle(UpdateSubtaskCommand request, CancellationToken cancellationToken)
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

        Result updateResult = task.UpdateSubtask(request.SubtaskId, request.Title?.Trim(), request.IsDone);
        if (updateResult.IsFailure)
        {
            return Result.Failure<SubtaskResponse>(updateResult.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        Subtask subtask = task.Subtasks.Single(s => s.Id == request.SubtaskId);
        return Result.Success(TaskMappings.ToResponse(subtask));
    }
}

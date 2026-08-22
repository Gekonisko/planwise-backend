using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Subtasks;

internal sealed class RemoveSubtaskCommandHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<RemoveSubtaskCommand>
{
    public async Task<Result> Handle(RemoveSubtaskCommand request, CancellationToken cancellationToken)
    {
        ProjectTask? task = await taskRepository.GetAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure(TaskErrors.NotFound(request.TaskId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(task.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure(TaskErrors.NotFound(request.TaskId));
        }

        task.RemoveSubtask(request.SubtaskId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

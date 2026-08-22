using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.ReorderTasks;

internal sealed class ReorderTasksCommandHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<ReorderTasksCommand, IReadOnlyList<TaskResponse>>
{
    public async Task<Result<IReadOnlyList<TaskResponse>>> Handle(ReorderTasksCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(request.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<TaskResponse>>(TaskErrors.ProjectNotFound(request.ProjectId));
        }

        IReadOnlyList<ProjectTask> tasks = await taskRepository.GetByIdsAsync(request.ProjectId, request.TaskIds, cancellationToken);
        if (tasks.Count != request.TaskIds.Count)
        {
            return Result.Failure<IReadOnlyList<TaskResponse>>(TaskErrors.InvalidReorderSet(request.ProjectId));
        }

        var tasksById = tasks.ToDictionary(task => task.Id);
        for (int i = 0; i < request.TaskIds.Count; i++)
        {
            tasksById[request.TaskIds[i]].Reorder((i + 1) * 1024m);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        IReadOnlyList<TaskResponse> responses = request.TaskIds.Select(id => TaskMappings.ToResponse(tasksById[id])).ToList();
        return Result.Success(responses);
    }
}

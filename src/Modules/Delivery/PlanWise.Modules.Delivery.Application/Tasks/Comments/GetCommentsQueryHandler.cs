using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Comments;

internal sealed class GetCommentsQueryHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUserContext userContext)
    : IQueryHandler<GetCommentsQuery, IReadOnlyList<CommentResponse>>
{
    public async Task<Result<IReadOnlyList<CommentResponse>>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        ProjectTask? task = await taskRepository.GetAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<IReadOnlyList<CommentResponse>>(TaskErrors.NotFound(request.TaskId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(task.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<CommentResponse>>(TaskErrors.NotFound(request.TaskId));
        }

        IReadOnlyList<CommentResponse> responses = task.Comments
            .OrderBy(comment => comment.CreatedAtUtc)
            .Select(TaskMappings.ToResponse)
            .ToList();

        return Result.Success(responses);
    }
}

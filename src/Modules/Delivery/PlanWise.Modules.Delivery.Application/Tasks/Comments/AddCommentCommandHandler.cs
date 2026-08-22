using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Common.Application.Clock;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Comments;

internal sealed class AddCommentCommandHandler(
    IProjectTaskRepository taskRepository,
    IProjectAccessService projectAccessService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddCommentCommand, CommentResponse>
{
    public async Task<Result<CommentResponse>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        ProjectTask? task = await taskRepository.GetAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<CommentResponse>(TaskErrors.NotFound(request.TaskId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(task.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<CommentResponse>(TaskErrors.NotFound(request.TaskId));
        }

        TaskComment comment = task.AddComment(userId, request.Body.Trim(), dateTimeProvider.UtcNow);
        taskRepository.AddComment(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(TaskMappings.ToResponse(comment));
    }
}

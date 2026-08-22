using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Comments;

public sealed record AddCommentCommand(Guid TaskId, string Body) : ICommand<CommentResponse>;

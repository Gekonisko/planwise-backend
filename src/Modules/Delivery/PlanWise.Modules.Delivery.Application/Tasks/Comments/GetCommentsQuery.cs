using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.Comments;

public sealed record GetCommentsQuery(Guid TaskId) : IQuery<IReadOnlyList<CommentResponse>>;

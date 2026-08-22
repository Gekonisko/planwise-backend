using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Delivery.Application.Tasks;

namespace PlanWise.Modules.Delivery.Application.Tasks.GetBoard;

public sealed record GetBoardQuery(Guid ProjectId) : IQuery<BoardResponse>;

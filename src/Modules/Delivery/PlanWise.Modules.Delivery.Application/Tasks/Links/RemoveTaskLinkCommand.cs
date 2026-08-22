using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Delivery.Application.Tasks.Links;

public sealed record RemoveTaskLinkCommand(Guid TaskId, Guid LinkId) : ICommand;

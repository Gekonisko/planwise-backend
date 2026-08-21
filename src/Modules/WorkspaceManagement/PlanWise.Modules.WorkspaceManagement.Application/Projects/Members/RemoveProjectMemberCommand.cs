using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

public sealed record RemoveProjectMemberCommand(Guid ProjectId, Guid UserId) : ICommand;
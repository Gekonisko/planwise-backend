using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

public sealed record RemoveProjectMemberCommand(Guid ProjectId, Guid MemberId) : ICommand;
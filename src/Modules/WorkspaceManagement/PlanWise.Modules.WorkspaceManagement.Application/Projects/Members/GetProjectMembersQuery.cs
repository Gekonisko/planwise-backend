using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

public sealed record GetProjectMembersQuery(Guid ProjectId) : IQuery<IReadOnlyList<ProjectMemberResponse>>;

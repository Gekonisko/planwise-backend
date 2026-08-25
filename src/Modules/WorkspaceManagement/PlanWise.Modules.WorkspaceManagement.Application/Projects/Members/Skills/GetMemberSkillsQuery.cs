using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members.Skills;

public sealed record GetMemberSkillsQuery(Guid MemberId) : IQuery<SkillsResponse>;

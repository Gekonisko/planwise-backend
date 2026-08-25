using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members.Skills;

public sealed record SetMemberSkillsCommand(Guid MemberId, IReadOnlyList<string> Skills) : ICommand<SkillsResponse>;

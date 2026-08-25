namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members.Skills;

public sealed record SkillsResponse(Guid MemberId, IReadOnlyList<string> Skills);

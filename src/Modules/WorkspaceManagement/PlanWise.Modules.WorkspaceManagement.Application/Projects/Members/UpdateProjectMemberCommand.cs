using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

public sealed record UpdateProjectMemberCommand(
    Guid ProjectId,
    Guid MemberId,
    string Role,
    decimal Capacity,
    decimal HourlyRate) : ICommand<ProjectMemberResponse>;

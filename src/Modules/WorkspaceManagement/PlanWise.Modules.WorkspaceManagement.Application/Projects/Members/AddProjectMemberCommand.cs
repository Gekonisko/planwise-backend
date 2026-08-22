using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

public sealed record AddProjectMemberCommand(
    Guid ProjectId,
    Guid? UserId,
    string Email,
    string Role,
    decimal Capacity,
    decimal HourlyRate) : ICommand<ProjectMemberResponse>;
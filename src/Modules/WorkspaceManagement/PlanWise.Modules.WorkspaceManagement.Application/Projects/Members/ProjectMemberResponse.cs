namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Members;

public sealed record ProjectMemberResponse(Guid UserId, string Email, string Role, decimal Capacity, decimal HourlyRate);
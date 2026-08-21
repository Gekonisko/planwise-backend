namespace PlanWise.Modules.WorkspaceManagement.Application.Projects;

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string KeyPrefix,
    string Process,
    string Status,
    Guid OwnerId,
    int MemberCount,
    int LabelCount);
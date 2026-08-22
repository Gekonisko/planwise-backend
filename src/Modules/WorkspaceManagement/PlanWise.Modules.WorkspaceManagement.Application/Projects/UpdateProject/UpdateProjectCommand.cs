using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.WorkspaceManagement.Application.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    string? Name,
    string? Process,
    string? ClientName,
    string? Status) : ICommand<ProjectResponse>;
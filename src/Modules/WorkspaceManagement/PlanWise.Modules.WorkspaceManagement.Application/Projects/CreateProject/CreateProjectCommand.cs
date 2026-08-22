using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.WorkspaceManagement.Application.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string KeyPrefix,
    string Process,
    string? ClientName) : ICommand<ProjectResponse>;
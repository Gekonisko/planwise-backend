using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;
using PlanWise.Modules.WorkspaceManagement.Application.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.UpdateProject;

public sealed record UpdateProjectCommand(Guid ProjectId, string Name, string Process) : ICommand<ProjectResponse>;
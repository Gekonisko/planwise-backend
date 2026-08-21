using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;
using PlanWise.Modules.WorkspaceManagement.Application.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.CreateProject;

public sealed record CreateProjectCommand(string Name, string KeyPrefix, string Process) : ICommand<ProjectResponse>;
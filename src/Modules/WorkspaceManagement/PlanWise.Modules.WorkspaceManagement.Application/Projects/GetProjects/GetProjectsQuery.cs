using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.GetProjects;

public sealed record GetProjectsQuery : IQuery<IReadOnlyList<ProjectResponse>>;
using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.GetProjects;

public sealed record GetProjectsQuery : IQuery<IReadOnlyList<ProjectResponse>>;
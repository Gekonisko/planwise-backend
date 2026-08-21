using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.GetProject;

public sealed record GetProjectQuery(Guid ProjectId) : IQuery<ProjectResponse>;
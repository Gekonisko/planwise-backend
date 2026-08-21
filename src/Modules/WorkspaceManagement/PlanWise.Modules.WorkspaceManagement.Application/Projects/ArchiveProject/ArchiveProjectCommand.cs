using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.ArchiveProject;

public sealed record ArchiveProjectCommand(Guid ProjectId) : ICommand;
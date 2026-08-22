using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.ArchiveProject;

public sealed record ArchiveProjectCommand(Guid ProjectId) : ICommand;
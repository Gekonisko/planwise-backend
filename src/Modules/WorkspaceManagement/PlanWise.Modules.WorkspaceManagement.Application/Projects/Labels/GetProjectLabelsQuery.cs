using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Messaging;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.Labels;

public sealed record GetProjectLabelsQuery(Guid ProjectId) : IQuery<IReadOnlyList<LabelResponse>>;
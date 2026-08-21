using PlanWise.Common.Domain;

namespace PlanWise.Modules.WorkspaceManagement.Domain.Projects;

public sealed class ProjectLabel : Entity
{
    private ProjectLabel()
    {
    }

    private ProjectLabel(Guid projectId, string name, string color)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Name = name;
        Color = color;
    }

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }
    public string Color { get; private set; }

    public static ProjectLabel Create(Guid projectId, string name, string color) =>
        new(projectId, name, color);
}
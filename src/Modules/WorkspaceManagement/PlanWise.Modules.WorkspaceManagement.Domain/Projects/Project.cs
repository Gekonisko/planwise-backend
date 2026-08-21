using PlanWise.Common.Domain;

namespace PlanWise.Modules.WorkspaceManagement.Domain.Projects;

public sealed class Project : Entity
{
    private readonly List<ProjectMember> members = [];
    private readonly List<ProjectLabel> labels = [];

    private Project()
    {
    }

    private Project(string name, string keyPrefix, string process, Guid ownerId)
    {
        Id = Guid.NewGuid();
        Name = name;
        KeyPrefix = keyPrefix;
        Process = process;
        OwnerId = ownerId;
        Status = ProjectStatus.Active;
    }

    public string Name { get; private set; }
    public string KeyPrefix { get; private set; }
    public string Process { get; private set; }
    public ProjectStatus Status { get; private set; }
    public Guid OwnerId { get; private set; }
    public IReadOnlyCollection<ProjectMember> Members => members;
    public IReadOnlyCollection<ProjectLabel> Labels => labels;

    public static Project Create(string name, string keyPrefix, string process, Guid ownerId) =>
        new(name, keyPrefix, process, ownerId);

    public void Update(string name, string process)
    {
        Name = name;
        Process = process;
    }

    public void Archive() => Status = ProjectStatus.Archived;

    public ProjectMember AddMember(Guid userId, string email, string role, decimal capacity, decimal hourlyRate)
    {
        var member = ProjectMember.Create(Id, userId, email, role, capacity, hourlyRate);
        members.Add(member);
        return member;
    }

    public void RemoveMember(Guid userId) => members.RemoveAll(member => member.UserId == userId);

    public ProjectLabel AddLabel(string name, string color)
    {
        var label = ProjectLabel.Create(Id, name, color);
        labels.Add(label);
        return label;
    }
}

public enum ProjectStatus
{
    Active,
    Archived
}
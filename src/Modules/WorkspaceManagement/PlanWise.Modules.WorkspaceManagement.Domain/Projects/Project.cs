using PlanWise.Common.Domain;

namespace PlanWise.Modules.WorkspaceManagement.Domain.Projects;

public sealed class Project : Entity
{
    private readonly List<ProjectMember> members = [];
    private readonly List<ProjectLabel> labels = [];

    private Project()
    {
    }

    private Project(string name, string keyPrefix, string process, Guid ownerId, string? clientName)
    {
        Id = Guid.NewGuid();
        Name = name;
        KeyPrefix = keyPrefix;
        Process = process;
        OwnerId = ownerId;
        ClientName = clientName;
        Status = ProjectStatus.Active;
    }

    public string Name { get; private set; }
    public string KeyPrefix { get; private set; }
    public string Process { get; private set; }
    public string? ClientName { get; private set; }
    public ProjectStatus Status { get; private set; }
    public Guid OwnerId { get; private set; }
    public IReadOnlyCollection<ProjectMember> Members => members;
    public IReadOnlyCollection<ProjectLabel> Labels => labels;

    public static Project Create(string name, string keyPrefix, string process, Guid ownerId, string? clientName = null) =>
        new(name, keyPrefix, process, ownerId, clientName);

    public void Update(string? name, string? process, string? clientName)
    {
        if (name is not null)
        {
            Name = name;
        }

        if (process is not null)
        {
            Process = process;
        }

        if (clientName is not null)
        {
            ClientName = clientName;
        }
    }

    public void ChangeStatus(ProjectStatus status) => Status = status;

    public void Archive() => Status = ProjectStatus.Archived;

    public ProjectMember AddMember(Guid? userId, string email, string role, decimal capacity, decimal hourlyRate)
    {
        var member = ProjectMember.Create(Id, userId, email, role, capacity, hourlyRate);
        members.Add(member);
        return member;
    }

    public void UpdateMember(Guid memberId, string role, decimal capacity, decimal hourlyRate) =>
        members.SingleOrDefault(member => member.Id == memberId)?.Update(role, capacity, hourlyRate);

    public void SetMemberSkills(Guid memberId, IReadOnlyList<string> skills) =>
        members.SingleOrDefault(member => member.Id == memberId)?.SetSkills(skills);

    public void RemoveMember(Guid memberId) => members.RemoveAll(member => member.Id == memberId);

    public bool ClaimMembership(string email, Guid userId)
    {
        ProjectMember? pending = members.SingleOrDefault(member =>
            member.UserId is null && string.Equals(member.Email, email, StringComparison.OrdinalIgnoreCase));

        if (pending is null)
        {
            return false;
        }

        pending.LinkUser(userId);
        return true;
    }

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
    OnHold,
    Completed,
    Archived
}
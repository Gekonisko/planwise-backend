using PlanWise.Common.Domain;

namespace PlanWise.Modules.WorkspaceManagement.Domain.Projects;

public sealed class ProjectMember : Entity
{
    private ProjectMember()
    {
    }

    private ProjectMember(Guid projectId, Guid? userId, string email, string role, decimal capacity, decimal hourlyRate)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        UserId = userId;
        Email = email;
        Role = role;
        Capacity = capacity;
        HourlyRate = hourlyRate;
    }

    public Guid ProjectId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Email { get; private set; }
    public string Role { get; private set; }
    public decimal Capacity { get; private set; }
    public decimal HourlyRate { get; private set; }
    public string[] Skills { get; private set; } = [];

    public static ProjectMember Create(Guid projectId, Guid? userId, string email, string role, decimal capacity, decimal hourlyRate) =>
        new(projectId, userId, email, role, capacity, hourlyRate);

    public void Update(string role, decimal capacity, decimal hourlyRate)
    {
        Role = role;
        Capacity = capacity;
        HourlyRate = hourlyRate;
    }

    public void LinkUser(Guid userId) => UserId ??= userId;

    public void SetSkills(IReadOnlyList<string> skills) => Skills = skills.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
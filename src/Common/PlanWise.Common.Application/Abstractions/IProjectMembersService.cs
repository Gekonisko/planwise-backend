namespace PlanWise.Common.Application.Abstractions;

public interface IProjectMembersService
{
    Task<IReadOnlyList<ProjectMemberSummary>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default);
}

public sealed record ProjectMemberSummary(Guid? UserId, string Email, decimal Capacity);

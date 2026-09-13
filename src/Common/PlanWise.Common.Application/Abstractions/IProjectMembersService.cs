namespace PlanWise.Common.Application.Abstractions;

public interface IProjectMembersService
{
    Task<IReadOnlyList<ProjectMemberSummary>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default);
}

// Role and HourlyRate are what the project actually pays for this person, so they are the only
// honest basis for a cost estimate — see IRateCardProvider, which derives the rate card from them.
public sealed record ProjectMemberSummary(
    Guid? UserId,
    string Email,
    decimal Capacity,
    IReadOnlyList<string> Skills,
    string Role,
    decimal HourlyRate);
